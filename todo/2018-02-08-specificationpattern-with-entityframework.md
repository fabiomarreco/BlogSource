---
layout: post
title: "Using the Specification Pattern with EntityFramework"
date: 2018-02-08 16:38:39
comments: true
keywords: "C#, domain driven design, ddd, entity framework, specification"
categories: Design Patterns
tags:
- Design Pattern
- Visitor
- Specification
- EntityFramework
- C#
---

# Using the Specification Pattern with EntityFramework

Temp:

Vladimir Khorikov has an excelent [course on pluralsight](https://www.pluralsight.com/courses/csharp-specification-pattern) where he explores specification techniques with EntityFramework. There is also a transcribed version [here](http://enterprisecraftsmanship.com/2016/02/08/specification-pattern-c-implementation/). 


In a previous post, I´ve 



We have shown how we can use a [Specification](2017-12-18-a-generic-specification-pattern-in-c) to encapsulate complex logics, specially useful when developing filtering capabilities in the application. In short, a simple interface `ISpecification` handles any potential messy logic you might have:

```csharp
public interface ISpecification<T>
{
    bool Specification IsSatisfiedBy(T item);
}
```

However the example presented was simplified to access a in-memory list containing the objects that are being filtered. If you try to implement it on a real-world application, filtering objects in-memory may not be a suitable solution. 
So we need to be able to send the specification to the infrastructure layer. Conventionally, it´ll be a relational database. In the following example I´ll be using *Entity Framework* as the means to access it. However, the example should be quite similar for any ORM. 

> *Vladimir Khorikov* has an excellent [article](http://enterprisecraftsmanship.com/2016/02/08/specification-pattern-c-implementation/) about the specification pattern which I highly recommend. However, his solution adds a new method to the `ISpecification<T>` interface which will probably just be useful for querying the database (although decoupled from it). I believe this violates the *Single Responsibility* principle. Also, he is using the same class of the domain model and on EF which highly un-recommend on larger systems, making it harder to add behavior and invariants to the entity, thus corrupting the domain model. 

Datasets on the entity framework implement `IQueryable<T>`, meaning it has methods that take an expression tree (such as `IQueryable<T>.Where(Expression<Func<T, bool>>)`) and is able handle those expression in a way other than executing immediately in-memory. On Entity Framework´s case, it can translate the expression tree into a SQL Query to be used against the database.

Remembering the [previous post](2018-01-01-generic-visitor-pattern-in-c), whe have dived into 


Defined:
```csharp
ublic interface ISpecificationVisitor<TVisitor, T> : where TVisitor : ISpecificationVisitor<TVisitor, T>
{
    void Visit(AndSpecification<T, TVisitor> spec);
    void Visit(OrSpecification<T, TVisitor> spec);
    void Visit(NotSpecification<T, TVisitor> spec);
}

public interface ISpecification <in T, in TVisitor> : where TVisitor : ISpecificationVisitor<TVisitor, T>
{
    bool IsSatisfiedBy(T item);
    void Accept (TVisitor visitor);
}


public interface IProductSpecificationVisitor : ISpecificationVisitor<IProductSpecificationVisitor, Product>
{
    void Visit (ProductMatchesCategory spec);
    void Visit (ProductPriceInRange spec);
}

public class ProductMatchesCategory : ISpecification<Product, IProductSpecificationVisitor>
{
    // ..other methods.. //

    public void Accept (IProductSpecificationVisitor visitor) 
    {
        visitor.Visit(this); // Now it compiles!
    }
}
```
...

However, using the `Product` class which represents a Aggregate on our domain model as the class used by the entity framework to persist is a bad idea, 
 - We cannot enforce invariants on Entity Framework (EF) classes.
    if a business rule states that products with no category should not be visibe.
 in c# that is easy as 
 ```csharp
 public class Product
 {
     //...
     public bool IsVisible { get; private set; }
     public Category Category { get; private set; }
     public SetCategory (Category category)
     {
         isVisible = (category != Category.Empty)
         Category = category
     }
 }
 ```
howerver, ef clases must have all properties with public setters, and would not make it possible to enforce this rule

Also the mapping from the database schema into the class might not be straight forward. A expert in Entity Framework might be able to bypass most of these limitations, but not without sacrifying the integrity of the domain model a little bit.
//example

I prefer to have separate classes for the domain model and persistence. With that in mind, they alse belong to different places in the application. The domain class belongs to the central layer (in a hexagonal architecture), while the ef class belongs to a outer layer (infrastructure)..

Looking at our ISpecification, it is clearly also part of the domain model (along withe the domain class). So adding a new method to that specification that returns a expression  of the Ef class (as *Vladimir Khorikov*  proposes), should not be possible (because the ef class is at the infrastructures layer).

that is when the visitor presented before becomes handy. whe should be able to add a new functionality to the specification to create a expression based on our specification for example:


```csharp
public class ProductEFExpressionVisitor : IProductSpecificationVisitor
{
    Expression<Func<EFProduct, bool>> expr = () => true;
    
    void Visit(AndSpecification<T, TVisitor> spec)
    {
        expr = 
    }
    
    void Visit(OrSpecification<T, TVisitor> spec)
    {

    }
    
    void Visit(NotSpecification<T, TVisitor> spec)
    {

    }
    
    void Visit (ProductMatchesCategory spec)
    {

    }
    
    void Visit (ProductPriceInRange spec)
    {

    }
}
```

//...
show usage

-----
start explaning the bad idea to use efclass & domainclass as the same.
give example with the product class

remember using specification to filter in-memory. what if we want to filter in-database ?
explain ef uses expressions 

should be nice to add a method on specification class to return expression of entity, that is bad. (talk about *Vladimir Khorikov* post)

visitor can add necessary behaviour


show example;
.