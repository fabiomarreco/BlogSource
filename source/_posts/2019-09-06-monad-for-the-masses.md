---
layout: post
title: "Monad for the masses"
name: "monad-4-masses"
date: 2019-12-01
comments: true
keywords: "C#, FP, Functional, Monad"
description: "A pragmatic monad introduction to c# developers"
category: Functional Programming
tags:
- Functional Programming
- C#
- Monads
---

# Monad for the masses

If you came to this post, you probably already heard the dreadful word _MONADS_ somewhere. Maybe you are trying to make your code `more functional` as seems to be the hype now, maybe you are using lambdas, linq queries, immutability etc. and want to take the next step. 

I would probably argue that the next step would be to play around in a language that supports [curring](https://fsharpforfunandprofit.com/posts/currying/), [partial applications](https://fsharpforfunandprofit.com/posts/partial-application/) and [sum types](https://fsharpforfunandprofit.com/posts/discriminated-unions/). (I´ve talked about those using F# in a [previous post]({{ site.baseurl }}{% post_url 2018-04-16-financial-modelling-in-fsharp-part-1 %}) ). But if you are still not ready to take the jump (to a functional language) and are curious about this monad thingy, fear not. This post is for you. 

>I´ll be using C#, but the concepts can be used in any statically typed language that supports parametric polymorphism (the `<T>` in `Repository<T>` for example). So... golang people can leave now. (kidding).

## What is a monad, then?

Easy:
> _"Monads are just monoids in the category of endofunctors"_

Satisfied ?

This is what we get in many explanations out there. The problem is, unless you are a category theory mathematician this holds little meaning. This is indeed the strictly correct definition and is applicable far beyond programming, (albeit incomprehensible to most people). So I will not delve into the mathematical definition. Lets focus on its application to programming.

> If, however you are not afraid of a little math, I highly recommend Bartosz Milewski´s [Category Theory for Programmers](https://bartoszmilewski.com/2014/10/28/category-theory-for-programmers-the-preface/). It is free in [PDF](https://github.com/hmemcpy/milewski-ctfp-pdf) and [Kindle](https://github.com/onlurking/category-theory-for-programmers), or in [Hardcover](https://www.blurb.com/b/9621951-category-theory-for-programmers-new-edition-hardco). He also has several [Youtube video classes](https://www.youtube.com/playlist?list=PLqjxJs3NyH72EvfeePfMfUJgBJcspSNYX).

So to programmers, monads can be a powerful abstraction that lets us compose work while always abiding to some strict behavior.

I know it still too abstract but don´t worry, It will soon become more clear with examples. But know this: All you need to implement a monad are 3 functions `Lift`, `Map` and `FlatMap` (which we will define latter on). 

Some usages of monads are:

- Error handling
- Transform & operate on sequences and lists
- Inject Dependencies
- Handle side effects (such as logging or state) in a pure function
- Lazy code evaluation
- Optional Values
- Parsing

And the list goes on. In fact, once you get conformable with it, you will start seeing monads everywhere. We will start with error handling example and see how it goes.

## Unhandled Exception 

We all know OOP languages have built in error handling mechanism called exceptions. But truth being told, sometimes they can be a huge pain. It is never clear when a method might throw an exception or why. The type of the exception may be anything. And even with java´s throws declaration, error handling is still awkward. 

Functional languages try to aim at functions that are not only _"pure"_ (have no side effects) but that are also _"total"_. Meaning that for every input there should be an defined output. Throwing exceptions is a type of result that was not defined in the function´s signature. Notice the following function, for example:

```csharp
public T GetItemIndex<T> (T[] array, int index) { 
    return array[index];
}
```

When we try to pass an `index` greater then the array´s length (or negative number), this function blows up with a `IndexOutOfRangeException`, therefore this function is undefined for some inputs. This function is not _total_.

## Making it total

We can make make it total by enhancing the result type to contain the error case: 

```csharp
public class Result<T>
{
    public Result(string errorDescription)
    {
        IsSuccess = false;
        ErrorDescription = errorDescription;
    }

    public Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    public static Result<T> Error(string description) => new Result<T>(description);
    public static Result<T> Success(T result) => new Result<T>(result);

    public bool IsSuccess { get; }
    public T Value { get; }
    public string ErrorDescription { get; }
}

public Result<T> GetItemIndex<T> (T[] array, int index) { 
    if (index < 0 )
        return Result<T>.Error("Index should be greater then zero");

    if (index >= array.Length )
        return Result<T>.Error("Index is greater then the array size");

    return Result<T>.Success(array[index]);
}
```

Now the function is always defined.

> I´ve added an ErrorDescription as string, but you could use an enumeration, or no description at all, It´s up to you.

It is nice that it is a generic type, and can be used in many different scenarios, such as retrieving from database, or anything else that might result in error.

## Real world mess

When we look at real world examples where we need to combine results from different sources, the usage of the class above can become less then ideal. Imagine a simple retail store domain were in order to add a product to a basket, where we need to:

1. Retrieve the product from the database
2. Reserve the product (so that we guarantee we have it in stock)
3. Add the reserved product to a customer basket

Ideally, we would have a function such as (in pseudo-code)

``` js
function AddToBasket(productId, customerId)
{
    product = productRepository.GetProduct(productId);
    productReservation = inventory.ReserveProduct(product);
    customer = customerRepository.GetCustomer(customerId);
    newBasket = customer.Basket.WithProductReservation(productReservation);
    return newBasket
}
```

However each of these steps can fail. If we throw in some error handling, things get a lot uglier:

```csharp

public interface IProductRepository
{
    Result<Product> GetProductId(string id);
}

public interface IInventory
{
    Result<ProductReservation> ReserveProduct(Product product);
}

public interface IBasket
{
    IBasket WithProductReservation(ProductReservation reservation);
}

public interface ICustomerRepository
{
    Result<Customer> GetCustomerId(string customerId);
}

//.........
public Result<IBasket> AddToBasket(string productId, string customerId)
{
    var productResult = _productRepository.GetProductId(productId);
    if (!productResult.IsSuccess)
        return Result<IBasket>.Error(productResult.ErrorDescription);
    var product = productResult.Value;

    var reservationResult = _inventory.ReserveProduct(product);
    if (!reservationResult.IsSuccess)
        return Result<IBasket>.Error(reservationResult.ErrorDescription); 
    var reservation = reservationResult.Value;


    var customerResult = _customerRepository.GetCustomerId(customerId);
    if (!customerResult.IsSuccess)
        return Result<IBasket>.Error(customerResult.ErrorDescription);
        
    var basket = customerResult.Value.Basket;

    var newBasket = basket.WithProductReservation(reservation);
    return Result<IBasket>.Success(newBasket);
}
```

Code is now entangled with error handling. Exceptions can also get very messy with lots of `try/catch`, especially if you need to take action on each error. 


## Parameterize with functions

First, let´s take a look at the last part of our `AddToBasket` function: 

```csharp
if (!customerResult.IsSuccess)
    return Result<IBasket>.Error(customerResult.ErrorDescription);
    
var basket = customerResult.Value.Basket;

var newBasket = basket.WithProductReservation(reservation);
return Result<IBasket>.Success(newBasket);
```

From the `Result` perspective, it is simply applying a transformation to the customer if it exists, or short circuiting in case of error. In fact, we can extract that logic and define a function to do just that. Let´s call that function `Map`


```csharp
public class Result<T>
{
    //...
    public Result<T2> Map<T2>(Func<T, T2> fn)
    {
        if (!IsSuccess)
            return Result<T2>.Error(this.ErrorDescription);

        var value2 = fn(Value);
        return Result<T2>.Success(value2);
    }
} 
```

We can now make the original code more readable:

```csharp
    return customerResult.Map(c=> c.Basket.WithProductReservation(reservation));
```

Oneliner! Now we are getting somewhere. Can we apply the same strategy to other pieces of the code? Let´s try with the first part:

```csharp
var productResult = _productRepository.GetProductId(productId);
if (!productResult.IsSuccess)
    return Result<IBasket>.Error(productResult.ErrorDescription);
var product = productResult.Value;

var reservationResult = _inventory.ReserveProduct(product);
if (!reservationResult.IsSuccess)
    return Result<IBasket>.Error(reservationResult.ErrorDescription); 
var reservation = reservationResult.Value;
```

Will then become: 

```csharp
Result<Result<ProductReservation>> reservation = productResult.Map(p => _inventory.ReserveProduct(p));
```

Huumm.. almost. We need now someway to *flatten* the `Result<Result<T>>` into `Result<T>`. In fact `Map` and then `Flatten` will be so common, we might as well create a function that does both at the same time. Let´s call it `FlatMap`:

```csharp
public class Result<T>
{
    //...
    public Result<T2> FlatMap<T2>(Func<T, Result<T2>> fn)
    {
        if (!IsSuccess)
            return Result<T2>.Error(this.ErrorDescription);

        return fn(Value);
    }
}
```

Thus, the function will become:

```csharp
public Result<IBasket> AddToBasket(string productId, string customerId)
{
    var productResult = _productRepository.GetProductId(productId);
    var reservationResult = productResult.FlatMap(p => _inventory.ReserveProduct(p));
    var customerResult = _customerRepository.GetCustomerId(customerId);
    var result = reservationResult
                    .FlatMap(reservation => 
                        customerResult.Map(c=> c.Basket.WithProductReservation(reservation)));
    return result;
}
```

Congratulations, you have now (inadvertently) defined a *Monad*. Remember that I said monads need only 3 functions? Let´s recap:

* **Lift**: Takes a value and *lifts* it into a monad. We have defined a surrogate for it. It´s the `Success` function. It takes any valid `T` value and creates a `Result<T>`.
* **Map**: Transforms the content (of type `T`) with a function `Func<T, T2>` into a `Result<T2>`. Something like `Result<T2> Map(Func<T, T2> fn)`. Note that if the `Result` is an error , this functions does nothing, just propagates the error. 
* **FlatMap**: It is the function that give the monad a _composable_ capability. It will create a `Result<T2>` given a method `Func<T, Result<T2>>`. Or `Result<T2> FlatMap(Func<T, T2> fn)`. Sometimes it is called `Bind` or `fmap`

> Defining only the functions **Lift** and **Map** give you the so called _Functor_. It is a useful abstraction, and sometimes it is all you need. Functors however, are not composable you will need a `FlatMap` thus having a _Monad_.

## Making it pretty

Some functional languages have a way to express monads in a native way. Look at the `AddToBasket` method in F#

```fsharp
let addToBasket productId customerId = result { 
    let! product = _productRepository.GetProductId(productId)
    let! reservation = _inventory.ReserveProduct(product )
    let! customer = _customerRepository.GetCustomerId(customerId);
    return customer.Basket.WithProductReservation(reservation)
 } 
```

The `result` keyword says that everything inside is a _computation expression_ over `Result<>`, and the `let!` keyword (instead of the usual `let`) is a syntax sugar for the `FlatMap` (in F# it is actually called `Bind`). And this is useful to create very expressive code and push boilerplate to somewhere behind the scenes.

Other functional languages have something similar like Haskell´s `do` notation or Scala´s `for` comprehension.

C#´s `async`/`await` use the same idea to make the _monad_ `Task<T>` easy to use. Unfortunately It only works for tasks.

However, there is something about how LINQ is implemented that few people seems to realize, take a look at the following LINQ Query:

```csharp
var purchaseReport = 
    from customer in customers
    from purchase in customer.Purchases
    select new {
        CustomerName = customer.Name,
        ProductName = purchase.Name,
        ProcutPrice = purchase.Price
    }
```
This is actually a syntax sugar for: 

```csharp
var purchaseReport =
    customers
        .SelectMany(
            customer => 
                customer.Purchases
                    .Select(purchase => new {
                        CustomerName = customer.Name,
                        ProductName = purchase.Name,
                        ProductPrice = purchase.Price }));
```

And the methods `SelectMany` and `Select` are defined on the `Enumerable` extension class:

```csharp
public static IEnumerable<TResult> Select<TSource,TResult>(
    this IEnumerable<TSource> source,
    Func<TSource, TResult> selector);

public static IEnumerable<TResult> SelectMany<TSource,TResult>(
    this IEnumerable<TSource> source,
    Func<TSource,IEnumerable<TResult>> selector);
```

Take your time to take a good look at those signatures, and compare them with `Map` and `FlatMap`. Any resemblance? That is because sequences are also _monads_!. In fact, if we define similar extension functions to our `Result<>` we can hijack LINQ´s syntax to have our own monadic computation expression in C#.

Define the following class:
```csharp
public static class Result
{
    public static Result<TResult> Select<TSource, TResult>(
            this Result<TSource> m, Func<TSource, TResult> f)
        => m.Map(f);

    public static Result<TResult> SelectMany<TSource, TResult>(
        this Result<TSource> m, Func<TSource, Result<TResult>> f)
        => m.FlatMap(f);

    /*
        *   This function is required by linq for optimization reasons, you can define it 
        * in a very mechanical way as bellow
        */
    public static Result<TResult> SelectMany<TSource, TM, TResult>(
        this Result<TSource> m, Func<TSource, Result<TM>> mSelector, Func<TSource, TM, TResult> rSelector)
        => m.FlatMap(v => mSelector(v).Map(tm => rSelector(v, tm)));
}
```

With the above extension in scope, we can rewrite our `AddToBasket` method as

```csharp
public Result<IBasket> AddToBasketV3(string productId, string customerId)
{
    var result = 
        from product in _productRepository.GetProductId(productId)
        from reservation in _inventory.ReserveProduct(product)
        from customer in _customerRepository.GetCustomerId(customerId)
        select customer.Basket.WithProductReservation(reservation);
        
    return result;
}
```

Which is actually doing error handling instead of the usual list/sequence projection. The code also looks very clean, which is nice. We can even define nice-to-have methods like

```csharp
public static class Result
{
    //....
    public static Result<T> Try<T>(Func<T> fn)
    {
        try
        {
            var value = fn();
            return Result<T>.Success(value);
        }
        catch(Exception ex)
        {
            return Result<T>.Error(ex.Message);
        }
    }
}
``` 

So we can use on 3rd party libraries like:
```csharp
var result = 
    from dataReader in  Result.Try(() => sqlCommand.Execute())
    from product in ReadProduct(dataReader)
    //...
```

### tl;dr

Monads are a powerful abstraction that lets us compose pieces of codes with some behind-the-scenes state and control flow. Most functional languages have native support for creating and using them. Oddly enough, C# also has a built-in way of expressing them, albeit verbose and originally thought only for sequences.

We managed to create a Result monad to express functions that might return a failure. I´ve saved a [gist](https://gist.github.com/fabiomarreco/a9af5b466f080662aa22df8a3047975e) containing this code and will eventually post my own full implementation of the Result monad (with also other useful monads like Maybe, Reader, etc.).

I´m am not saying you should always use this technique, exceptions do have their place. But if you would like to use it, there is a library called [Chessie](http://fsprojects.github.io/Chessie/) that is pretty good (it is implemented in F#, but it works great using from a C# code base).


If you would like to dig a little deeper, I highly recommend Scott Wlaschin´s [Railway Oriented Programming blog series](https://fsharpforfunandprofit.com/rop/).
