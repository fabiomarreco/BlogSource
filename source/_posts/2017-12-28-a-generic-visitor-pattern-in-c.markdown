---
layout: post
title: "A Generic Visitor Pattern in C#"
description: "A Generic Visitor Pattern in C#"
date: 2017-12-28
comments: true
keywords: "C#, domain driven design, ddd, specification, visitor"
category: Design Patterns
tags:
- Design Pattern
- Visitor
- Specification
- C#
---

# A Generic Visitor Pattern in C#

On the [previous](2017-12-18-a-generic-specification-pattern-in-c) post, we showed how to implement a specification pattern in C#. In short, we have an interface `ISpecification<T>` which states if an object satisfy the specification.

```csharp
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T item);
}
```

That is very useful and all, eventually the specification will we have to deal with not-so-fun issues like serializing, translating to SQL or even generating a human-readable representation of the specification.
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

 This if effectively another design pattern called [Composite](https://en.wikipedia.org/wiki/Composite_pattern) as each part of the expression tree has uniform behavior.
 Every time we talk about Composites, the **Visitor** pattern should come to mind.


