---
layout: post
title: "A Generic Specification Pattern in C#"
description: "The specification design pattern allows encapsulation of complex logic. We will introduce a version using generics where we´ll be able to reuse boolean operators over any specification."
date: 2017-12-18
comments: true
keywords: "C#, domain driven design, ddd, specification"
category: Design Patterns
tags:
- Design Pattern
- Specification
- C#
---

# A Generic Specification Pattern in C#

## The Pattern

An important design pattern with extreme resonance in *Model Driven Design* is the **Specification**. It enables encapsulation of complex business logics in a simple interface. 
In complex systems, it is common when trying to simplify business logic, go at least halfway this pattern.

In few words, we will have an interface `ISpecification` with a single method, returning boolean indicating if a object is satisfied by the specification. 
There are many use cases, a classic one being filtering. For example, suppose we have an online store with many products, such as:

| ProductID |  ProductName   |  Price  |  Category   |
| --------- | -------------- | ------- | ----------- |
| 0001      | Blue Jeans     | $30.00  | Clothes     |
| 0002      | T-Shirt        | $24.99  | Clothes     |
| ...       | ...            | ...     | ..          |
| 0213      | Cell Phone     | $499.99 | Electronics |
| 0214      | TV             | $699.99 | Electronics |
| 0215      | LED Flashlight | $5.00   | Electronics |

And suppose we want to add a new feature to our online store, giving the ability to the user to filter the products based on certain criteria. The straight forward approach would be to add specific methods to our **repository** *(design pattern that we will cover latter)*. 

For example, if we want filter based on the product category, we could add a `GetProductsWithCategory` method as such:

```csharp
public class ProductRepository 
{
    private List<Product> _items;
    //...//

    public IEnumerable<Product> GetProductsWithCategory(string category)
    {
        return _items.Where(c=> c.Category == category);
    }
}
```

I´m accessing a in-memory list in this case, but it could be accessing a SQL database or some other store. That is not the point. It is to be taken as an example. You could argue in this example, that we could have a method as `IQueryable<Product> GetItems()` and filter the data on the client. While that might be true for a simple in-memory list, if we were using an EntityFramework-like ORM, that would force us to expose the EF query capabilities to the client, and that is a bad design. Not all LINQ queries can be translated to a SQL query, and we would need to rely on the developer to create a valid query.

That works fine. Now if we need to add new search features, such as finding products at a price range, or with name containing a particular string, we would need a method for each one of those:


```csharp
public class ProductRepository 
{
    private List<Product> _items;
    //...//

    public IEnumerable<Product> GetProductsWithCategory(string category)
    // { .. }
    public IEnumerable<Product> GetProductsWithPriceRange(decimal lowerBound, decimal upperBound)
    // { .. }
    public IEnumerable<Product> GetProductsWithNameContaining(string substring)
    // { .. }
}
```

You see where this is going? We´re clogging the repository with methods for each new capability of the search feature. As a consequence, this class is violating the Open/Closed principle, which states that we should be open for extension, but closed for modification (since we have to modify the class for each new feature).

Instead, let´s imagine we could **specify** which products we are interested in, and pass that `Specification` to our repository, reducing to a single method:

```csharp
public class ProductRepository 
{
    private List<Product> _items;
    //...//

    public IEnumerable<Product> GetProducts(IProductSpecification specification)
    {
        foreach (var product in _items)
            if (specification.IsSatisfiedBy(product))
                yield return product;
    }
}
```

Where the `IProductSpecification` interface would look like this:

```csharp
public interface IProductSpecification
{
    bool Specification IsSatisfiedBy(Product products);
}
```

We will want to use this pattern in different types of entities, not only `Products` right? In C# we can do that easily by making specification a generic type:

```csharp
public interface ISpecification<T>
{
    bool Specification IsSatisfiedBy(T item);
}
```

With the `GetProducts` method of the repository looking like this:

```csharp
public IEnumerable<Product> GetProducts(ISpecification<Product> specification)
{
    foreach (var product in _items)
        if (specification.IsSatisfiedBy(product))
            yield return product;
}
```

Now we can implement each new search capability as a separate class:

```csharp
public class ProductMatchesCategory : ISpecification<Product>
{
    public string Category { get; set; }
    public ProductMatchesCategory (string category)
    {
        this.Category = category;
    }
    public bool IsSatisfiedBy(Product item) => item.Category == this.Category;
}

public class ProductPriceInRange : ISpecification<Product>
{
    public decimal LowerBound { get; set; }
    public decimal UpperBound { get; set; }
    public ProductMatchesCategory (decimal lowerBound, decimal upperBound)
    {
        this.LowerBound = lowerBound;
        this.UpperBound = upperBound;
    }
    public bool IsSatisfiedBy(Product item) => (item.Price >= LowerBound) && (item.Price <= UpperBound);
}
```

To the distrait eye, it might seem cumbersome to create a class for each possible filter. And it might be so for very simple models, but as the complexity of the system grows, I can guarantee that trying to keep up with multiple methods of the repository, will far exceed the complexity of this pattern.

> There are many ways to implement this pattern. Eric Evans explores different scenarios on his famous [Blue Book](https://www.amazon.com.br/Domain-Driven-Design-Tackling-Complexity-Software/dp/0321125215). If you have a SQL database using Stored Procedures for example, you would need a different strategy. 


## Composite

Since specifications have the same signature, we can play around to combine them using boolean operators, which will really make this pattern shine. Let´s start with the main operators `And`, `Or` and `Not`:

```csharp
public class AndSpecification<T> : ISpecification<T>
{
    public ISpecification<T> Left { get; set; }
    public ISpecification<T> Right { get; set; }
    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        Left = left;
        Right = right;
    }
    public bool IsSatisfiedBy(T item) => Left.IsSatisfiedBy(item) && Right.IsSatisfiedBy(item);
}

public class OrSpecification<T> : ISpecification<T>
{
    public ISpecification<T> Left { get; set; }
    public ISpecification<T> Right { get; set; }
    public OrSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        Left = left;
        Right = right;
    }
    public bool IsSatisfiedBy(T item) => Left.IsSatisfiedBy(item) || Right.IsSatisfiedBy(item);
}

public class NotSpecification<T> : ISpecification<T>
{
    public ISpecification<T> Specification { get; set; }
    public OrSpecification(ISpecification<T> specification)
    {
        Specification = specification;
    }
    public bool IsSatisfiedBy(T item) => !Specification.IsSatisfiedBy(item);
}
```

Leveraging the features of C#, we can create an extension in order to make its usage more fluent:

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

That would enable us to create more complex specifications like this:

```csharp
new ProductPriceInRange(0M, 100M)
    .And (new ProductMatchesCategory("Clothes")
            .Or(new ProductMatchesCategory("Electronics")));
```

That example shows how you can use it to create a filtering feature. However, the specification pattern is a lot more powerful than that.

Following the previous example, suppose that we want our customers to be notified when there is a price change on a given product. It shouldnt be a broadcast to all users, but rather a subset of users that meet certain criteria, such as:

- Users that have previously purchased products from the same category.
- Users that have *starred* the current product 
- Only users that have been active on the previous year

As developers, we know for a fact that management will want to change those rules eventually, so we need a solution that allows extensibility without changing existing code. The *Specification Pattern* fits those requirements perfectly.

```csharp
ISpecification<User> spec = 
    new UserPurchasedProductWithCategory(product.Category)
    .Or(new UserWithStarredProduct(product))
    .And(new UserActiveSince(DateTime.Today.AddYears(-1)))

userNotificationService.Notify(spec, message);
```

Pretty cool, right ? On next part, we´ll try to improve the extensibility of the specification by using a Visitor pattern, while still keeping the specification generic.


> Vladimir Khorikov has an excelent [course on pluralsight](https://www.pluralsight.com/courses/csharp-specification-pattern) where he explores specification techniques with EntityFramework. There is also a transcribed version [here](http://enterprisecraftsmanship.com/2016/02/08/specification-pattern-c-implementation/). 


