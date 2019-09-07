---
layout: post
title: "Monad for the masses"
name: "monad-4-masses"
date: 2019-09-06
comments: true
keywords: "C#, FP, Functional, Monad"
description: "The dreadfull & scary monads in programming might be simpler then you think."
category: Functional Programming
tags:
- Functional Programming
- C#
- Monads
---

# Monad for the masses

I´ve been meaning to write this blog post about monads as a way to help me improve my ability to explain monads to the unintiated.

If came to this post, you probably already heard the dreadfull word _MONADS_ somewhere. Maybe you are trying to make your code `more functional` as seems to be the hype now, are using lambdas, linq queries, immutability etc. and want to take the next step. 

I would probably argue that the next step would be to play arround in a language that supports [curring](https://fsharpforfunandprofit.com/posts/currying/), [partial applications](https://fsharpforfunandprofit.com/posts/partial-application/) and [sum types](https://fsharpforfunandprofit.com/posts/discriminated-unions/). (I´ve talked about those using F# in a [previous post]({{ site.baseurl }}{% post_url 2018-04-16-financial-modelling-in-fsharp-part-1 %}) ). But if you are still not ready to take the jump and are curious about this monad thinggy, fear not, this post is for you. 

>I´ll be using C#, but the concepts can be used in any statically typed language that supports parametric polimorphism (the `<T>` in `Repository<T>` for example). So... golang people can leave now. (kidding).

## What is a monad, then?

Easy:
> _"Monads are just monoids in the category of endofunctors"_

Satisfied ?

This is what we get in many explanations out there. The problem is, unless you are a cathegory theory mathematician this holds little meaning. This is the strictly correct definition applicable far beyond programming, but incomprehensible to most people. So I will not delve into the mathematical definition. Lets focus on its application to programming.

> If you are not afraid of a little math, I highly recommed Bartosz Milewski´s [Category Theory for Programmers](https://bartoszmilewski.com/2014/10/28/category-theory-for-programmers-the-preface/). It is free in [PDF](https://github.com/hmemcpy/milewski-ctfp-pdf) and [Kindle](https://github.com/onlurking/category-theory-for-programmers), or in [Hardcover](https://www.blurb.com/b/9621951-category-theory-for-programmers-new-edition-hardco). He also has several [Youtube video classes](https://www.youtube.com/playlist?list=PLqjxJs3NyH72EvfeePfMfUJgBJcspSNYX).

So to programmers, monads are a powerfull abstraction that lets us compose work while allways abiding to some strict behaviour.

Still too abstract ? don´t worry, It will become more clear with examples. But know this: All you need to implement a monad are 3 functions `Lift`, `Map` and `Bind` (which we will define latter on). 

Some usages of monads are:

- Error handling
- Transform & operate on sequences and lists
- Inject Dependencies
- Handle side efects (such as logging or state ) in a pure function
- Lazy code evaluation
- Optional Values
- Parsing

And the list goes on. In fact, once you get confortable with it, you will start seeing monads everywhere. We will start with error handling example and see how it goes.

## Unhandled Exception 

We all know OOP languages have built in error handling mechanism called exceptions. But sometimes, they can be a huge pain. It is never clear when a method might throw an exception or why. The type of the exception may be anything. And even with java´s 


```csharp
// a function transform a type to the monad
Monad<T> Lift(T item); 

// a function to transform
Monad<TResult> Map<T, TResult>(Func<T, TResult> f); 

Mon
```