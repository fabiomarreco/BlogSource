---
layout: post
title: "Financial modelling in F# Part 1 - Interest Rates"
permalink: financial-modelling-in-fsharp-part-1
date: 2018-04-16 15:43:35
comments: true
description: "Financial modelling in F# - Part 1"
keywords: "F#, finance"
categories: Modelling
tags:
- Finance
- FSharp
- Modelling
- Beginner
---

# Financial modelling in F# - Part 1 (Interest Rates)

I work developing a financial application that is used by financial institutions such as hedge funds and investment banks. One common assignment is to model financial contracts, and calculate many analytics upon them, such as pricing valuation or risk measures. The application is developed in C#. However, I´ve found that F# is a great language to model financial applications, and I use it almost daily to test the application or when creating a new model.

So now I´ve decided to share a little of the modelling. The expressiveness of the type system of the language is astonishing, especially if you com from a Object-Oriented language like myself. OO is bloated with boilerplate code that we unfortunately fall accustomed to like Factories, Visitors, etc. and these constructs are way simpler, or simply don´t make sense in functional languages.

>There seems to be a bridge people have to cross in order to learn Functional Programing (FP). However, Scott Wlaschin shows in his excellent book [Domain Modeling made functional](https://www.amazon.com/Domain-Modeling-Made-Functional-Domain-Driven/dp/1680502549) that FP can be crystal clear for domain modeling, and well understood even by non-technical people

When modelling a financial application, it is good to understand who is your target user. Finance is a vast subject with often conflicting concepts. An investment bank for example, will have many different areas, such as Front-Office(Traders), Risk-Management, Back-Office & Accounting, M&A among others. If you try to create a single model to grasp all existing concepts (buy/sell, cashflows, trades, transfers, assets, etc.) you are bound to fail. Complexity will grow fast, and the model will have to support many different use case scenarios. Eventually the patchwork will be too hard to maintain, and even small changes will cause unpredictable consequences on the overall system. This is where the concept of [Bounded Context](https://martinfowler.com/bliki/BoundedContext.html) comes to play.

We will eventually look into some of these bounded contexts, and see with examples how to model them. However, there is a core concept of interests and accrual that we can start playing with. Why not start with interest rates?

## Defining Interest Rate

[Investopedia](https://www.investopedia.com/terms/i/interestrate.asp#ixzz5DFCuY8dw) defines interest rates as:

>*An interest rate is the amount of interest due per period, as a proportion of the amount lent, deposited or borrowed (called the principal sum). The total interest on an amount lent or borrowed depends on the principal sum, the interest rate, the compounding frequency, and the length of time over which it is lent, deposited or borrowed. It is defined as the proportion of an amount loaned which a lender charges as interest to the borrower, normally expressed as an annual percentage.*

Ok, there are a lot of concepts embedded in there. I´ll try to explain them a little, although I will not dive into too much detail because there are plenty of resources online for that. Our goal is to model the interest rate for a financial application while learning a little bit of F# as beginners.

Basically there are 2 ways to accrue interest: *Simple* and *Compounded*:

$$ Total Interest_{simple} = Principal * (Interest Rate_{simple} * Period  - 1) $$
$$ Total Interest_{compounded} = Principal * [(1+Interest Rate_{compounded})^{Period} - 1)] $$

However, the last formula above assumes that the the interest is being compounded at the same basis as the interest rate (usually annually). But sometimes, even though the interest rate is being expressed as *annual percentage* (basis), it can be compounded more frequently (say semiannually). In that case, the formula goes to: 

$$ T_{comp.} = P * \left [ \left (1+\frac{i_{comp.}}{ N } \right )^{t\over{N}} - 1 ) \right] $$

For readability sake, I´ve defined: `T = TotalInterest`, `P = Principal`, `i = Interest rate`, `t = Period` and `N = number of compoundings per basis period (usually annual)`

When you think about it, when someone says *Interest rate of 10%*, that has little meaning, since there are a lot of assumptions you will have to make to truly use that in a calculation. From a software development perspective, using a `double` or `decimal` to represent an interest rate is just as meaningless. 

In fact, it can even induce errors. Suppose a junior programmer is looking at the code base and finds somewhere a `decimal interest`. What is that ? is it Total Interest, or Interest rate ? If the former, is it simple or compounded ? Are all the parameters needed to calculate being passed correctly ? How is the `Period` being calculated ?

That is where a [Type Driven Design](https://fsharpforfunandprofit.com/posts/designing-with-types-intro/) approach comes to play (which tbh, is just part of Domain Driven Design usual recommendations). We should build expressive types, which make illegal states unrepresentable.

## Designing with types

>In order to not overengineer at this stage, I´ll ignore the rate basis, and assume we are talking about annual rate, as it usually is. We´ll probably refactor it in future posts when we find it to be useful.

It is clear that *Interest Rates* need at *least* these Information to be complete:

- **Compound:** Assumptions about the interest accrual, it can either be:
    - *Simple* 
    - *Compounded*: if so, it can either be:
        -  *periodic*: compounding "n" times over the basis period
        -  *continuous*: compounding infinite times over the basis period
- **Daycount Convention**: Since `Period` must be represented as a fraction of the Basis(year), how do we calculate it from 2 given dates? (e.g. How many years between 03/jan/2018 and 30/nov/2022?)

So let´start with the basics. In F# there are basically 2 types of structures possible (not completely true, but bear with me). **And** types and **Or** types.

 In **And** types, all fields are simultaneously required. It is easily understood by OOP programmers and in F# are called *Records*, and declared as follows:

```fsharp
type InterestRate = {
    Rate : decimal
    Compound: Compound;
    Daycount : DaycountConvention
}
```
 But this is assuming the types `Compound` and `DaycountConvention` already exists. In F# there is a little trick the community uses when they start modelling the domain. We create a *type alias* called `Undefined` so we can start having a compilable code base before we complete the model definition (this is important when using *REPL*).

 ```fsharp
type Undefined = Exception
//...
type Compound = Undefined;
type DaycountConvention = Undefined;
 ```

> I´ll not talk about tooling and IDE. But  I highly recommend using Ionide with Visual Studio Code. There is a quick start [here](https://docs.microsoft.com/en-us/dotnet/fsharp/get-started/get-started-vscode?tabs=windows).

The **Or** type on the other hand, represent a list of possible values. Each of which can be associated with other types. Think of it as an Enum on steroids. In F#, they are called *Discriminated Unions*.

```fsharp
type CompoundFrequency = 
    | Annually
    | Monthly
    | Daily
    | Continuous

type Compound = 
    | Simple
    | Compounded of CompoundFrequency
```

The last piece of the puzzle is the `DaycountConvention`. In order to actually use the interest rate, you´ll need to known how many years (or fraction of years) there are between two given dates. There are a few conventions that are commonly used. And we will describe them in following post. [Wikipedia](https://en.wikipedia.org/wiki/Day_count_convention) has a good list of conventions explaining them in detail. Here are some that I use more frequently:

```fsharp
type DaycountConvention = 
    | DC30E360
    | DC30360US
    | DCACT360
    | DCACT365
    | DCACTACTISDA
    | DCBUS252
```

The `DC` prefix stands for **D**aycount**C**onvention. This is just because some of them start with numbers which would be an invalid name.

From here, an usual approach is to organize these types into modules, and create a factory function. (I´ll separate the daycount convention into another module on another post)

```fsharp
module InterestRate = 
    type DaycountConvention = 
        | DC30E360
        | DC30360US
        | DCACT360
        | DCACT365
        | DCACTACTISDA
        | DCBUS252

    type CompoundFrequency = 
        | Annually
        | Monthly
        | Daily
        | Continous

    type Compound = 
        | Simple
        | Compounded of CompoundFrequency

    type InterestRate = {
        Rate : decimal
        Compound: Compound;
        Daycount : DaycountConvention
    }

    //DaycountConvention -> Compound -> decimal -> InterestRate
    let create daycount compound rate =  {
        Rate = rate;
        Compound = compound;
        Daycount = daycount
    }

    //Helper method to create treasury rates
    let treasury = create DCACTACTISDA Simple

    //Helper method for CDI-related interest rates (very common in brazil)
    let cdi = create DCBUS252 (Compounded Annually)
```

At the end of the module, I´ve created methods for commonly used interest rates, which are very useful. Of course we can add a lot more then that as we need it. We can then create a treasury rate as easily as

```fsharp
InterestRate.treasury 0.2M;;
```

yielding

```fsharp
val it : InterestRate.InterestRate = {Rate = 0.2M;
                                      Compound = Simple;
                                      Daycount = DCACTACTISDA;}
```

I just want to draw your attention to they way `treasury` method is implemented. It is calling the `create` function, but only passing 2 parameters, instead of the required 3.

```fsharp
let create daycount compound rate = //...
//...
let treasury = create DCACTACTISDA Simple // rate = ?
```

This is a side effect from a useful feature in Functional Programing called [*Currying*](https://en.wikipedia.org/wiki/Currying), and most OOP developers have a hard time around it.

In Lambda Calculus (and FP), functions have only **one input** and **one output**. The *one output* part is easy... No out parameters for you!.

The *one input* seems weird. How come "one" parameter if we declared the `create` method with 3 parameters: `let create daycount compound rate`  ? The answer lies in the signature that the compiler generates for this function: 

```fsharp
create: DaycountConvention -> Compound -> decimal -> InterestRate
```

This means, that the `create` method is actually a method that receives a `DaycountConvention` and returns another function, that receives `Compound` and returns yet another function that receives a `decimal` and returns an `InterestRate`.

The cool thing about it, is that you are no longer obligated to bind all parameters at once when referencing the function, and it opens up a whole world of composability. That means that when I declare the `treasury` function as: 

```fsharp
let treasury = create DCACTACTISDA Simple
```
I´m declaring that `treasury` is the same as `create DCACTACTISDA Simple` which is a function that receives a decimal (rate). Of course, I could very well be explicit like:

```fsharp
let treasury rate = create DCACTACTISDA Simple rate
```

It´s the same thing, there is literally no diference. But the ability do *partial application* on functions will be very important when we start combining them.


## Wraping up

This post was aimed at F# noobs who want to use it on financial applications. It´s far from explaining either finance or functional programming. The goal was to organize and explain a little library I use. Hopefully, the usefulness will be more evident in future posts when other pieces start falling together. However, the very essence of modelling with F# is there. Start describing the problem space, and design structures accordingly. 
Unlike OOP, behavior is separated from data structures, which is actually a refreshment because really... mixing behavior and data is not very obvious.
In future posts we will organize the code base a little better with modules and projects.