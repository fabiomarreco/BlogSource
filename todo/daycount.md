#Financial Modeling in F# Part 2 - Daycount Conventions


I´ve taken some time off since the last post, but as promised we´ll get a little forward on our Finance domain modeling in F#. Again, I´ll try to keep it directed to F#/FP beginners who want to use it on financial applications, explaining step-by-step.

In the [last post]({{ site.baseurl }}{% post_url 2018-04-16-financial-modelling-in-fsharp-part-1 %}), we saw that an interest rate is made of `Compound` and `DaycountConvention`

So this post will be about how to represent **Daycount Convention**. With this seemly simple requirement, we will actually be able to look into very cool features of the language. I cannot deny I´m a F# fanboy, and hopefully you will see that (besides first impressions), it is a very comfortable language to work with (specially within this domain).

## Daycount convention

Remember the formula for compounded interest from the previous post:

$$ Total Interest_{compounded} = Principal * [(1+Interest Rate_{compounded})^{Period} - 1)] $$

The key parameter here is `Period`. It is amount of time between two dates, represented as a fraction of the Basis(year).

The catch here is how to convert two dates into a fraction of a year. There are a number of conventions that are commonly used in finance, [Wikipedia](https://en.wikipedia.org/wiki/Day_count_convention) has a good list of them. Which one you should use depends on the financial contract you are trying to calculate or interest rate you are using.

I have already defined on the last post the type for the Daycount convention with the ones I use most:

```fsharp
type DaycountConvention = 
    | DC30E360
    | DC30360US
    | DCACT360
    | DCACT365
    | DCACTACTISDA
    | DCBUS252
```

As the naming suggests, these conventions have two parts: 
- `daysBetween`: Method to calculate number of days between two dates.
- `daysInYear`: Number of days to consider in a given year

For example: with DC**ACT** **365** you should count the **Act**ual number of days between two dates, and divide by **365** (ignoring leap years).

Ok that is enough to get us started.


##Getting our hands dirty

Starting with the basics, we can see by the description above we are handling time in different measures (day, year). Most of us largely ignore this, and use a `int` or `decimal` to represent them. However, these measures have strict rules to adhere to:
- You cant add a 2 `years` + 3 `days`, it is an invalid operation
- To convert a `days` to `years` you have to divide by the number of days in a year.

Of course, you can ignore these rules and use a value type. But it is actually very nice to have the compiler notify simple (and common) mistakes. 
Domain Driven Design recommend us to use single [Value Objects](https://martinfowler.com/bliki/ValueObject.html) in these cases. Basically a wrapper arround the value type, exposing only the allowed operations. On C# it would look something like this.

```csharp
public sealed class Days : IComparable<Days>, IEquatable<Days>
{
	private int _days;
	public Days(int days)
	{
		_days = days;
	}

	public Days Add(Days days) => new Days(days._days + _days);
	public int CompareTo(Days other) => this._days.CompareTo(other._days);
	public bool Equals(Days other) => this._days.Equals(other._days);
	public static bool operator ==(Days d1, Days d2) => d1._days == d2._days;
	public static bool operator !=(Days d1, Days d2) => d1._days != d2._days;
	public override int GetHashCode() => _days.GetHashCode();
	public override bool Equals(object obj) => (obj is Days other)?this.Equals(other) : false;
}
```

Ok, that is a lot of code for something so simple. Tbh you would probably put all that boilerplate code on some base class, so it wouldnt look that bad. The same code on F# would look like this:
```fsharp
type Days = Days of int with
        static member (+) (Days d1, Days d2) = d1 + d2
```

---

Introduction
    * [X] ~~*Last post*~~ [2018-07-28]
    * [X] ~~*What is daycount convention*~~ [2018-07-28]
    * [X] ~~*Why f#*~~ [2018-07-28]

Development
    ~~Property based testing - ?~~
    Units of Measure (trabalhando com ano, mes e dia)
    DaysBetween & DaysInYearsBetween : assinatura dos métodos (definir tipo.)
    PatternMatching
    30/360 -> Active Pattern (end of month, etc)
    NDU -> Tail recursion

Conclusion
    Recap ?
    ??
