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

If you came to this post, you probably already heard the dreadfull word _MONADS_ somewhere. Maybe you are trying to make your code `more functional` as seems to be the hype now, are using lambdas, linq queries, immutability etc. and want to take the next step. 

I would probably argue that the next step would be to play arround in a language that supports [curring](https://fsharpforfunandprofit.com/posts/currying/), [partial applications](https://fsharpforfunandprofit.com/posts/partial-application/) and [sum types](https://fsharpforfunandprofit.com/posts/discriminated-unions/). (I´ve talked about those using F# in a [previous post]({{ site.baseurl }}{% post_url 2018-04-16-financial-modelling-in-fsharp-part-1 %}) ). But if you are still not ready to take the jump (to a functional language) and are curious about this monad thinggy, fear not, this post is for you. 

>I´ll be using C#, but the concepts can be used in any statically typed language that supports parametric polimorphism (the `<T>` in `Repository<T>` for example). So... golang people can leave now. (kidding).

## What is a monad, then?

Easy:
> _"Monads are just monoids in the category of endofunctors"_

Satisfied ?

This is what we get in many explanations out there. The problem is, unless you are a cathegory theory mathematician this holds little meaning. This is indeed the strictly correct definition and is applicable far beyond programming, (albait  incomprehensible to most people). So I will not delve into the mathematical definition. Lets focus on its application to programming.

> If you are not afraid of a little math, I highly recommed Bartosz Milewski´s [Category Theory for Programmers](https://bartoszmilewski.com/2014/10/28/category-theory-for-programmers-the-preface/). It is free in [PDF](https://github.com/hmemcpy/milewski-ctfp-pdf) and [Kindle](https://github.com/onlurking/category-theory-for-programmers), or in [Hardcover](https://www.blurb.com/b/9621951-category-theory-for-programmers-new-edition-hardco). He also has several [Youtube video classes](https://www.youtube.com/playlist?list=PLqjxJs3NyH72EvfeePfMfUJgBJcspSNYX).

So to programmers, monads are a powerfull abstraction that lets us compose work while allways abiding to some strict behaviour.

Still too abstract ? don´t worry, It will become more clear with examples. But know this: All you need to implement a monad are 3 functions `Lift`, `Map` and `FlatMap` (which we will define latter on). 

Some usages of monads are:

- Error handling
- Transform & operate on sequences and lists
- Inject Dependencies
- Handle side efects (such as logging or state) in a pure function
- Lazy code evaluation
- Optional Values
- Parsing

And the list goes on. In fact, once you get confortable with it, you will start seeing monads everywhere. We will start with error handling example and see how it goes.

## Unhandled Exception 

We all know OOP languages have built in error handling mechanism called exceptions. But truth beeing told, sometimes they can be a huge pain. It is never clear when a method might throw an exception or why. The type of the exception may be anything. And even with java´s throws declaration, error handling is still akward. 

Functional languages try to aim at functions that are not only _"pure"_ (have no side effects) but that are also _"total"_. That means that for every input, there should be an defined output. So throwing exceptions is a type of result that was not defined in the funciton´s signature. Notice the following function, for example:

``` cs
public T GetItemIndex<T> (T[] array, int index) { 
    return array[index];
}
```

When we try to pass a `index` greater then the array´s length (or negative number), this function blows up with a `IndexOutOfRangeException`, therefore this function is undefined for some inputs. This function is not _total_.

## Making it total

We can make make it total by enhancing the result type to contain the error case: 

``` cs 
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
        return new Result<T>.Error("Index should be greater then zero");

    if (index >= array.Length )
        return new Result<T>.Error("Index is greater then the array size");

    return Result<T>.Success(array[index]);
}
```

Now the function is allways defined.

> I´ve added an ErrorDescription as string, but you could use an enumeration, or no description at all, It´s up to you.

It is nice that it is a generic type, and can be used in many different scenarios, such as retrieving from database, or anything else that might result in an error.


## Real world mess

If we look at real world examples where we need to combine results from different sources, its usage can become less then ideal. Imagine a simple retail store domain were in order to add a product to a basket, we need to 

1. Retrieve the product from the database
2. Reserve the product (so that we garantee we have in stock)
3. Add to a customer basket

Idealy, we would have a function such as (in pseudocode)

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

``` cs

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
    Result<Customer> GetBasketForCustomer(string customerId);
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


    var customerResult = _customerRepository.GetBasketForCustomer(customerId);
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

``` cs
if (!customerResult.IsSuccess)
    return Result<IBasket>.Error(customerResult.ErrorDescription);
    
var basket = customerResult.Value.Basket;

var newBasket = basket.WithProductReservation(reservation);
return Result<IBasket>.Success(newBasket);
```

From the `Result` perspective, it is simply applying a transformation to the customer if it exists. In fact. we can define a function to do just that. Let´s call that function `Map`


``` cs
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

``` cs
    return customerResult.Map(c=> c.Basket.WithProductReservation(reservation));
```

Oneliner! Now we are getting somewhere. Can we apply the same strategy to other pieces of the code ? Let´s try with the first part:

``` cs
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

``` cs
Result<Result<ProductReservation>> productResult.Map(p => _inventory.ReserveProduct(p));
```

Huumm.. almost. We need now someway to *flatten* the `Result<Result<T>>` into `Result<T>`. In fact `Map` and then `Flatten` will be so common, we might as well create a function that does both at the same time. Let´s call them `FlatMap`:

```cs
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

``` cs
public Result<IBasket> AddToBasket(string productId, string customerId)
{
    var productResult = _productRepository.GetProductId(productId);
    var reservationResult = productResult.FlatMap(p => _inventory.ReserveProduct(p));
    var customerResult = _customerRepository.GetBasketForCustomer(customerId);
    var result = reservationResult
                    .FlatMap(reservation => 
                        customerResult.Map(c=> c.Basket.WithProductReservation(reservation)));
    return result;
}
```

Congratulations, you have now defined a *Monad*. Remember that I said monads need only 3 functions? Let´s recap:

* **Lift**: Takes a value and *lifts* it into a monad. We have defined a surrogate for it. It´s the `Success` function. It takes any valid `T` value and creates a `Result<T>`.
* **Map**: Transforms the content with a given function, something like `Result<T2> Map(F) 

