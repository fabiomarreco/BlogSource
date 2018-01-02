---
layout: post
title: Generic Visitor Pattern in C#
comments: true
keywords: "C#, domain driven design, ddd, specification, visitor"
category: Design Patterns
tags:
- Design Pattern
- Visitor
- Specification
- C#
---

# Generic Visitor Pattern in C#

In the [Previous post](2017-12-28-introduction-to-the-visitor-pattern-in-c) we have showed an introduction to the visitor pattern being used to traverse a [specification](2017-12-18-a-generic-specification-pattern-in-c) expression tree. Now I´ll try to show a more generic version of the visitor. Again using the *specification* as the visitee, but this time the generic version `ISpecification<T>`. This is a more advanced post, I advise being familiar with these patterns before continuing.


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

As an initial visitor solution would be 
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

However, that is hardly useful. We need to be specific about the concrete specification types. For instance, if we are talking about `ProductSpecification`, we should have a visitor like this:

```csharp
public interface IProductSpecificationVisitor : ISpecificationVisitor<T>
{
    void Visit (ProductMatchesCategory spec);
    void Visit (ProductPriceInRange spec);
}
```

But when we try to implement the specific product specifications:

```csharp
public class ProductMatchesCategory : ISpecification<Product>
{
    // .... //
    public void Accept (ISpecificationVisitor<Product> visitor) 
    {
        visitor.Visit(this); // compile-time error
    }
}
```

Since `ISpecificationVisitor<Product>` does not have the correct method to visit `ProductMatchesCategory` (it is implemented on the `IProductSpecificationVisitor`). Sure, we could fix this by simply typecasting the visitor, but It would feel like defeating the purpose of the visitor.


----

And the supporting  extension methods:

```csharp
public static class SpecificationExtensions
{
    public static ISpecification<T> And(this ISpecification<T> left, ISpecification<T> right)
    {
        return new AndSpecification<T> (left, right);
    }
    public static ISpecification<T> Or(this ISpecification<T> left, ISpecification<T> right)
    {
        return new OrSpecification<T> (left, right);
    }
    public static ISpecification<T> Not(this ISpecification<T> specification)
    {
        return new NotSpecification<T> (specification);
    }
}
```


As an initial solution we would 