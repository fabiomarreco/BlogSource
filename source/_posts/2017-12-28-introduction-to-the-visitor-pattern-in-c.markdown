---
layout: post
title: "Introduction to the Visitor Pattern in C#"
name: "intro-visitor-c"
date: 2017-12-28
comments: true
keywords: "C#, domain driven design, ddd, specification, visitor"
description: "The visitor design pattern is often used to navigate on a composite. While that is true, it also overcomes limitations on single-dispatch languages (like C#/Java)"
category: Design Patterns
tags:
- Design Pattern
- Visitor
- Specification
- C#
---

# Introduction to the Visitor Pattern in C#

On the [previous]({% post_url 2017-12-18-a-generic-specification-pattern-in-c %}) post, we showed how to implement a specification pattern in C#. In short, we have an interface `ISpecification<T>` which states if an object satisfy the specification.

```csharp
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T item);
}
```

That is very useful, but eventually the specification will we have to deal with not-so-fun issues like serializing, translating to SQL or even generating a human-readable representation of the specification.
Sure, we can pollute the interface with methods that should not be its primary goal:

```csharp
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T item);
    SqlCommand CreateSqlCommand();
    string GetDescription();
    //...
}
``` 

That is clearly violating the *Single Responsibility Principle*, there should be a better way.
A good hint comes up if we analyse the specification a little deeper. We showed that we can compose the specification with boolean operators such as `Or`, `And`, `Not`, etc:

```csharp
new ProductPriceInRange(0M, 100M)
    .And (new ProductMatchesCategory("Clothes")
            .Or(new ProductMatchesCategory("Electronics")));
```

That yields a expression tree like this:

{% plantuml %}
(And) --> (ProductPriceInRange:\n**0 - 100**)
(And) --> (Or)
(Or) --> (ProductMatchesCategory:\n**Clothes**)
(Or) --> (ProductMatchesCategory:\n**Electronics**)
{% endplantuml %}

 This if effectively another design pattern called [Composite](https://en.wikipedia.org/wiki/Composite_pattern) as each part of the expression tree has uniform behavior.and every time we talk about Composites, the **Visitor** pattern should come to mind.

The visitor lets us traverse a composite, taking action on each node that can be specific to each implementation without resorting to typecasting.


Letting the the specification example sit aside for a bit, let´s take a simpler example __*without generics*__ as `ProductSpecification`

```csharp
public interface IProductSpecification
{
    bool IsSatisfiedBy(Product item);
    void Accept(IProductSpecificationVisitor visitor);
}
```

And these implementations:

```csharp
public class ProductMatchesCategory : IProductSpecification
{
    public string Category { get; }
    public ProductMatchesCategory(string category)
    {
        this.Category = category;
    }
    public bool IsSatisfiedBy(Product item) => item.Category == this.Category;
    public void Accept(IProductSpecificationVisitor visitor) => visitor.Visit(this);
}

public class ProductPriceInRange : IProductSpecification
{
    public decimal LowerBound { get; }
    public decimal UpperBound { get; }
    public ProductPriceInRange(decimal lowerBound, decimal upperBound)
    {
        this.LowerBound = lowerBound;
        this.UpperBound = upperBound;
    }
    public bool IsSatisfiedBy(Product item) => (item.Price >= LowerBound) && (item.Price <= UpperBound);
    public void Accept(IProductSpecificationVisitor visitor) => visitor.Visit(this);
}

public class ProductAndSpecification : IProductSpecification
{
    public IProductSpecification Left { get; set; }
    public IProductSpecification Right { get; set; }
    public ProductAndSpecification(IProductSpecification left, IProductSpecification right)
    {
        Left = left;
        Right = right;
    }
    public bool IsSatisfiedBy(Product item) => Left.IsSatisfiedBy(item) && Right.IsSatisfiedBy(item);

    public void Accept(IProductSpecificationVisitor visitor) => visitor.Visit(this);
}
```

The visitor would have a method for each possible implementation, such as:

```csharp
public interface IProductSpecificationVisitor
{
	void Visit(ProductMatchesCategory spec);
	void Visit(ProductAndSpecification spec);
	void Visit(ProductPriceInRange spec);
	// .. and more methods for each possible specification
}
```

And instead of polluting `ISpecification` interface, we´re going to create a visitor implementation for each new behavior we want. If we want a description behavior, for example, we could create a visitor like this:

```csharp
public class ProductDescriptionSpecificationVisitor : IProductSpecificationVisitor
{
	public string Description { get; private set; } = string.Empty;

	public void Visit(ProductMatchesCategory spec)
		=> Description += $"Matches Category ({spec.Category})";

	public void Visit(ProductPriceInRange spec)
		=> Description += $"Price between {spec.LowerBound} and {spec.UpperBound}";

	public void Visit(ProductAndSpecification spec)
	{
		spec.Left.Accept(this);
		Description += " and ";
		spec.Right.Accept(this);
	}
}
```

and we use it like this:

```csharp
var descriptionVisitor = new ProductDescriptionSpecificationVisitor();
productSpecification.Accept(descriptionVisitor);
string description = descriptionVisitor.Description;
```

We call the specification´s `Accept` method passing in a visitor implementation, next the specification calls the `Visit` method of the Visitor(specific to the specification implementation). 

All this is required because C# like most OO language is *Single Dispatch*. Meaning, that the decision of which version of a method to call is based on the concrete type of the interface being called. There are other languages (like [julia](https://docs.julialang.org/en/stable/manual/methods/#man-methods) for example) are able to also choose different methods depending on the concrete type of the parameters which is called *Multiple Dispatch*.

{% plantuml %}
Visitor :.Visit(Specification)
Specification :.Accept(visitor)

Specification -> Visitor
{% endplantuml %}

>Actually, C# can behave as *Dynamic Dispatch* language since the `dynamic` keyword was introduced, but that can lead to runtime errors if not used carefully, so I´ll ignore it for now.


This was a brief introduction to the *Visitor Pattern*, on the next post we´ll create an implementation of this pattern that uses the Generics feature of C# in order to avoid code duplication.
