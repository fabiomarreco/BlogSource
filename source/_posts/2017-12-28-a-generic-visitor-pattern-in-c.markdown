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
    bool IsSatisfiedBy(T item)
}
```

We also showed that we can compose the specification with boolean operators such as `Or`, `And`, `Not`, etc:

```csharp
new ProductPriceInRange(0M, 100M)
    .And (new ProductMatchesCategory("Clothes")
            .Or(new ProductMatchesCategory("Electronics")));
```

{% plantuml %}
(And) --> (ProductPriceInRange:\n**0 - 100**)
(And) --> (Or)
(Or) --> (ProductMatchesCategory:\n**Clothes**)
(Or) --> (ProductMatchesCategory:\n**Electronics**)
{% endplantuml %}

 This behavior, if effectively another desig


