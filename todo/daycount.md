#Financial Modeling in F# Part 2 - Daycount Conventions


I´ve taken some time off since the last post, but as promised we´ll move forward on our finance domain modeling in F#. Again, I´ll try to keep it directed to F# & Functional Programming beginners who want to use it on financial applications, explaining it step-by-step.

In the [last post]({{ site.baseurl }}{% post_url 2018-04-16-financial-modelling-in-fsharp-part-1 %}), we saw that an interest rate is made of `Compound` and `DaycountConvention`

So this post will be about how to represent **Daycount Convention**. With this seemly simple requirement, we will actually be able to look into very cool features of the language. I cannot deny I´m a F# fanboy, and hopefully you will see that (maybe besides first impressions), it is a very comfortable language to work with (specially within this domain).

## Daycount convention

Remember the formula for compounded interest from the previous post:

$$ Total Interest_{compounded} = Principal * [(1+Interest Rate_{compounded})^{Period} - 1)] $$

The key parameter here is `Period`, and that is what we will be focusing on this post. It is amount of time between two dates, represented as a fraction of the Basis (usually a year).

The catch is how to convert two dates into a fraction of a year. There are a number of conventions that are commonly used in finance, [Wikipedia](https://en.wikipedia.org/wiki/Day_count_convention) has a good list of them. Which one you should use depends on the financial contract you are trying to calculate or the source of the interest rate you are using.

I have already defined on the last post the type for the Daycount convention with the ones I use most (as a discriminated union):

```fsharp
type DaycountConvention = 
    | DC30E360
    | DC30360US
    | DCACT360
    | DCACT365
    | DCACTACTISDA
    | DCBUS252
```

As the naming of each convention suggests, these conventions have two parts: 
- `DaysBetween`: How to calculate number of days between two dates.
- `DaysInYear`: Number of days to consider in a given year

For example: with DC**ACT** **365** you should count the **Act**ual number of days between two dates (subtracting), and then divide by **365** (ignoring leap years).

And that is enough to get us started.


##Getting our hands dirty

### Units of Measure

Starting with the basics, we can see by the description above we are handling time in different measures (day, year). Most of us largely ignore this, and use a `int` or `decimal` to represent them. However, these measures have strict rules to adhere to:
- You cant add 2 `years` + 3 `days`, it is an invalid operation
- To convert a `days` to `years` you have to divide by the number of days in a year.

Of course, you can ignore these rules and use a value type. But it is actually very nice to have the compiler notify simple (and common) mistakes. 

Domain Driven Design recommend us to use single [Value Objects](https://martinfowler.com/bliki/ValueObject.html) in these cases. Basically a wrapper around the value type, exposing only the allowed operations. On C# it would look something like this.

```csharp
public sealed class Days : IEquatable<Days>
{
	private int _days;
	public Days(int days)
	{
		_days = days;
	}

	public Days Add(Days days) => new Days(days._days + _days);
	public bool Equals(Days other) => this._days.Equals(other._days);
	public static bool operator ==(Days d1, Days d2) => d1._days == d2._days;
	public static bool operator !=(Days d1, Days d2) => d1._days != d2._days;
	public override int GetHashCode() => _days.GetHashCode();
	public override bool Equals(object obj) => (obj is Days other)?this.Equals(other) : false;
    //...
}
```
We could do something similar in F#, with A LOT less code. However, when we are talking about physical measures (meters, feet, BTU, days, etc.) there is a simpler way using (unsurprisingly) _Units of Measures_:

```fsharp
[<Measure>] type days
[<Measure>] type months
[<Measure>] type years
```  

These measures, define different numeric types that enforce type safety when doing arithmetic operations. Such as:

```fsharp
let days16 = 16<days>
//val days16 : int<days> = 16

let years2 = 2<years>
//val years2 : int<years> = 2

let somePeriod = days16 + years2
//error FS0001: The unit of measure 'years' does not match the unit of measure 'days'
```

You need to convert between them to use it:

```fsharp
let somePeriod = days16 + years2 * 365<days/years>
//val somePeriod : int<days> = 746
```

Very cool right ? It is also very fast, since it compiles down to simple numeric values.

### Pattern Matching

Now let´s try to implement the first part of the daycount convention, the one I´ve called `DaysBetween` (which calculates the number of days between two dates). On OOP you would probably implement it by defining an interface, and implement it on each of the possible daycount conventions. Although that can be done here, it is not idiomatic in F#.

We will start with the easy ones. Every DayCountConvention that starts with DC**ACT**, should calculate the `DaysBetween` them simply as the total actual days, which we can calculate using `DateTime.Subtract` method:

```fsharp
//actualDaysBetween : startDate:DateTime -> endDate:DateTime -> int<days>
let actualDaysBetween (startDate:DateTime) (endDate:DateTime) = 
    int ((endDate.Date.Subtract(startDate.Date).TotalDays)) *  1<days>
```

The compiler is not able to automatically infer the type of  `startDate` and `endDate`, so I had to specify them (as `DateTime`). I am also disregarding any "Time" information from DateTime (by using the `Date` property). Next, I am typecasting the result of `TotalDays` to int, and them attaching a unit of measure to it by multiplying by `1<days>`.

And this function can be used in any convention that uses actual days between. That can be stated with pattern matching as follows:

```fsharp
let daysBetween convention  =
    match convention with
    | DCACT360
    | DCACTACTISDA
    | DCACT365 -> actualDaysBetween

//warning FS0025: Incomplete pattern matches on this expression. 
//   For example, the value 'DC30360US' may indicate a case not covered by the pattern(s).
```
 
The signature of pattern matching is very straight forward. We will soon start diving into cooler features like active patterns. But for now, notice the warning the compiler has shown us. It states that we are not finished. There are conventions not covered by the function (which we will do next). This also gives us the guarantee that when we add a new convention, there won´t be any situation left untreated.

#### Active pattern

Next, for `DC30E360`, we will take a look at the [Wikipedia](https://en.wikipedia.org/wiki/Day_count_convention) link which states:

> For all 30*/360 methods:
> $ DayCount = 360 * (Y_2-Y_1) + 30 * (M_2 - M1)  + (D_2 - D_1) $
> **30E/360** specific Date adjustment rules:
> - If D1 is 31, then change D1 to 30.
> - If D2 is 31, then change D2 to 30.

We could implement it as this: 

```fsharp
let ``30EdaysBetween`` (startDate:DateTime) (endDate:DateTime) =
    let mutable d1 = startDate.Day 
    let mutable d2 = endDate.Day 
    let y1 = startDate.Year 
    let y2 = endDate.Year 
    let m1 = startDate.Month 
    let m2 = endDate.Month 

    if (d1 = 31) then d1 <- 30
    if (d2 = 31) then d2 <- 30

    let years  = (y2 - y1) * 1<years>
    let months = (m2 - m1) * 1<months>
    let days   = (d2 - d1) * 1<days>

    (years * 360<days/years>) + (months * 30<days/months>) + days
```
> F# lets us use names starting with numbers or spaces by using double brackets (` ``..`` `), but it is not allowed on discriminated union declaration.

We are adjusting the days as required, and this function works. This is something that we would do in C#/Java without blinking. However, the implementation above is not considered idiomatic. I´ve heard [Scott Wlaschin](https://twitter.com/scottwlaschin) say once that although F# doesn´t enforce you immutability as some functional languages do, it does force you down the walk of shame by using the `mutable` keyword and `<-` operator.

![Walk of Shame](img/let-mutable.gif#center)

Just avoid it unless you REALLY need it. And you usually don´t.

What do we do then? This problem just begs for pattern matching. For starters we can leverage a cool feature called active pattern:

```fsharp
let (|Date|) (date:DateTime) = (date.Year, date.Month, date.Day)
```

We are telling F# that it should be able to match a `DateTime` against `Date` with the tuple `(Year, Month, Day)`. For example:

```fsharp
match DateTime.Today with
| Date (_, 12, 25) -> printfn "Today is christmas! :)"
| _ -> printfn "NOT christmas! :("
```

The underline `_` is a match against anything (not previously matched).

> *Don´t worry if you dont fully understand how it works right now, it will get clearer as we go forward*.

Applying it to our previous function, we get:

---

```fsharp
let rec ``30EdaysBetween`` (d1:DateTime) (d2:DateTime) =
    match (d1, d2) with
    | ( Date (y1, m1, 31), _) ->  ``30EdaysBetween`` (DateTime(y1, m1, 30)) d2
    | ( _, Date (y2, m2, 31)) -> ``30EdaysBetween`` d1 (DateTime(y2, m2, 30))
    | _ ->  actualDaysBetween d1 d2
```

Looks indeed better. Notices that I have applied the active pattern `|Date|` inside a tuple `(d1, d2)`. So combining pattern matching is perfectly allowed.

Next convention on the list is `DC30360US` which wikipedia describes as:

>**30/360 US**
> Date adjustment rules (more than one may take effect; apply them in order, and if a date is changed in one rule the changed value is used in the following rules): 
> - If the investment is EOM and (Date1 is the last day of February) and (Date2 is the last day of February), then change D2 to 30.
> - If the investment is EOM and (Date1 is the last day of February), then change D1 to 30.
> - If D2 is 31 and D1 is 30 or 31, then change D2 to 30.
> - If D1 is 31, then change D1 to 30.

Ah conventions! What would it be of us programmers if there were no ad-hoc rules made up from random unknown people!

Luckly, active patterns are very useful in this scenario. We need a way to identify it is the last day of a month. The difference from the last case, is that this `EndOfMonth`  pattern might, or might not be matched. So we need a partial active pattern:

```fsharp
let (|EndOfMonth|_|) (date:DateTime) =
    if (date.AddDays(1.).Month <> date.Month) then Some date.Month
    else None
// ( |EndOfMonth|_| ) : date:DateTime -> int option
```

Notice the return type `int option` ? `Option<T>` in F# is a type that might or might not have a value. It is essentially declared as:

```fsharp
type Option<'a> = 
    | Some of 'a 
    | None
```

Back to our problem, I will define the _daysBetween_ for 30US convention as:

```fsharp
let rec ``30USdaysBetween`` (d1:DateTime) (d2:DateTime) =
    match (d1, d2) with
    | (EndOfMonth 2, EndOfMonth 2) -> ``30USdaysBetween`` d1 (d2.AddDays(float (30-d2.Day)))
    | (EndOfMonth 2, _) -> ``30USdaysBetween`` (d1.AddDays(float (30-d2.Day))) d2
    | (Date(_, _, 30), Date (y2, m2, 31)) 
    | (Date(_, _, 31), Date (y2, m2, 31)) -> ``30USdaysBetween`` d1 (DateTime(y2, m2, 30))
    | (Date(y1, m1, 31), _) -> ``30USdaysBetween`` (DateTime(y1, m1, 30)) d2
    | _ -> actualDaysBetween d1 d2

```





---

### Está bem explicado varios conceitos, que podem ser reaproveitados.....

Ok, that is a lot of code for something so simple. Tbh you would probably put all that boilerplate code on some base class, so it wouldnt look that bad. Event so, the same code on F# would look like this:
```fsharp
type Days = Days of int with
        static member (+) (Days d1, Days d2) = d1 + d2 |> Days
```

"What about equality?", you might ask. That is a given! F# has structural comparison as default.


Ok, that is a lot of code for something so simple. Tbh you would probably put all that boilerplate code on some base class, so it wouldnt look that bad. Event so, the same code on F# would look like this:
```fsharp
type Days = Days of int with
        static member (+) (Days d1, Days d2) = d1 + d2 |> Days
```

"What about equality?", you might ask. That is a given! F# has structural comparison as default.

However, as easy to read as it is, there are a few hidden concepts in there that I feel I should mention. The first `Days` is the name of the type, the second `Days` is the name of the *construcutor*. These names can be different, but conventionally a single case discriminated union has the same name as its constructor. Therefore, we can use the type as this:

```fsharp
let days32 = Days 32
//val days32 : Days = Days 32
```

The second important concept is called called *deconstruction*. It is commonly used in pattern-matching (which we will see soon). It helps us "unbox" a type by using the very same constructor

```fsharp
let (Days n) = days32
//val n : int = 32
```

Another hidden concept is Tuple. anything in a parenthesis, separated by comma in F# is a tuple, e.g: (3, 4) is a tuple of 2 ints

Therefore, `static member (+) (Days d1, Days d2)` as stated above is attaching a (static) function `(+)` that receives a tuple of 2 `Days`, and is *deconstructing* them inline, to expose their value.


---

Introduction
    * [X] ~~*Last post*~~ [2018-07-28]
    * [X] ~~*What is daycount convention*~~ [2018-07-28]
    * [X] ~~*Why f#*~~ [2018-07-28]

Development
    ~~Property based testing - ?~~
    * [X] ~~*Units of Measure (trabalhando com ano, mes e dia)*~~ [2018-09-22]
    DaysBetween & DaysInYearsBetween : assinatura dos métodos (definir tipo.)
    PatternMatching
    30/360 -> Active Pattern (end of month, etc)
    NDU -> Tail recursion

Conclusion
    Recap ?
    ??
