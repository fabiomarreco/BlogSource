---
layout: post
title: Generic Visitor Pattern in C#
comments: true
keywords: "C#, domain driven design, ddd, specification, visitor"
description: "The visitor pattern is hard to use when the visitee is a generic interface. We will present a solution using Specification<T> as example."
category: Design Patterns
tags:
- Design Pattern
- Visitor
- Specification
- C#
---

# Generic Visitor Pattern in C#

In the [Previous post]({{ site.baseurl }}{% post_url 2017-12-28-introduction-to-the-visitor-pattern-in-c %}) we have showed an introduction to the visitor pattern being used to traverse a [specification]({{ site.baseurl }}{% post_url 2017-12-18-a-generic-specification-pattern-in-c %}) expression tree. Now I´ll try to show a more generic version of the visitor. Again using the *specification* as the visitee, but this time the generic version `ISpecification<T>`. This is a more advanced post, I advise being familiar with these patterns before continuing.

We had the specification:

```csharp
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T item);
}
```

With the generic boolean operators:

```csharp
public class AndSpecification<T> : ISpecification<T> { /* .. */ }
public class OrSpecification<T> : ISpecification<T> { /* .. */ }
public class NotSpecification<T> : ISpecification<T> { /* .. */ }
```

Now, to create a generic visitor, we could as a first thought, create a base interface, containing at least the boolean operators, such as:

```csharp
public interface ISpecificationVisitor<T> 
{
    void Visit(AndSpecification<T> spec);
    void Visit(OrSpecification<T> spec);
    void Visit(NotSpecification<T> spec);
}

public interface ISpecification<T>
{
    bool IsSatisfiedBy(T item);
    void Accept(ISpecificationVisitor<T> visitor);
}
```

As we will see however, this does not solve the problem. The interface we use for the visitor cannot be extended with other concrete specification types. For instance, if we are talking about `ProductSpecification`, we could have a visitor interface like this:

```csharp
public interface IProductSpecificationVisitor : ISpecificationVisitor<Product>
{
    void Visit (ProductMatchesCategory spec);
    void Visit (ProductPriceInRange spec);
}
```

However, when we try to implement the specific product specifications:

```csharp
public class ProductMatchesCategory : ISpecification<Product>
{
    // ..other methods.. //

    public void Accept (ISpecificationVisitor<Product> visitor) 
    {
        visitor.Visit(this); // compile-time error!
    }
}
```

Since `ISpecificationVisitor<Product>` does not have the correct method to visit `ProductMatchesCategory` (it is only defined on the `IProductSpecificationVisitor`), this code will not compile. Sure we could fix this by simply typecasting the visitor, but It would introduce potential runtime errors (also feels like defeating the purpose of the visitor).

There is a little trick I´ve learned in C# when we need to know the inherited type at design level, useful when you do not want to loose strong type definition on the base interface.

First, we´ll create a generic type on the visitor interface representing the child interface itself. Bear with me:

```csharp
public interface ISpecificationVisitor<TVisitor, T> : where TVisitor : ISpecificationVisitor<TVisitor, T>
{
    void Visit(AndSpecification<T, TVisitor> spec);
    void Visit(OrSpecification<T, TVisitor> spec);
    void Visit(NotSpecification<T, TVisitor> spec);
}

public interface ISpecification <in T, in TVisitor> : where TVisitor : ISpecificationVisitor<TVisitor, T>
{
    void Accept (TVisitor visitor)
}
``` 

Take the time to assimilate these examples, we´re introducing a generic parameter `TVisitor` into the interface `ISpecificationVisitor` and requiring it to inherit from `ISpecificationVisitor`!. That means `TVisitor` is the inherited type itself!

That way, the `ProductSpecificationVisitor` will become:

```csharp
public interface IProductSpecificationVisitor : ISpecificationVisitor<IProductSpecificationVisitor, Product>
{
    void Visit (ProductMatchesCategory spec);
}
```

And the `ProductMatchesCategory`will be:

```csharp
public class ProductMatchesCategory : ISpecification<Product, IProductSpecificationVisitor>
{
    // ..other methods.. //

    public void Accept (IProductSpecificationVisitor visitor) 
    {
        visitor.Visit(this); // Now it compiles!
    }
}
```

And that works beautifully. We have a generic specification pattern and we´ll be able to reuse the boolean operators with any specification we want:

```csharp
public class AndSpecification<T, TVisitor> :ISpecification<T, TVisitor> where TVisitor : ISpecificationVisitor<TVisitor, T>
{
    public ISpecification<T, TVisitor> Left { get; }
    public ISpecification<T, TVisitor> Right { get; }

    public AndSpecification(ISpecification<T, TVisitor> left, ISpecification<T, TVisitor> right)
    {
        this.Left = left;
        this.Right = right;
    }

    public void Accept(TVisitor visitor) => visitor.Visit(this);
    public bool IsSatisfiedBy(T obj) => Left.IsSatisfiedBy(obj) && Right.IsSatisfiedBy(obj);
}
```
The same idea can be easily applied to the other boolean operators (`Or` & `Not`).

These are very powerful patterns, more so when used together. When we get the hang of it, they start showing up everywhere, and it is a great way segregate complex logics on our model. On the next post I´ll show examples of visitors for serializing or querying a database.