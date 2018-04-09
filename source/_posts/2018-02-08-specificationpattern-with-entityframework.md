---
layout: post
title: "Using the Specification Pattern with EntityFramework"
date: 2018-02-08 16:38:39
comments: true
keywords: "C#, domain driven design, ddd, entity framework, specification"
description: "The last post of the series, we´ll use a specification to query the database using EntityFramework while keeping domain/infrastructure models separated"
categories: Design Patterns
tags:
- Design Pattern
- Visitor
- Specification
- EntityFramework
- C#
---

# Using the Specification Pattern with EntityFramework

We have shown in a previous post how we can use a [Specification]({{ site.baseurl }}{% post_url 2017-12-18-a-generic-specification-pattern-in-c %}) to encapsulate complex logics, specially useful when developing filtering capabilities in the application. In short, a simple interface `ISpecification` handles any potential messy logic you might have:

```csharp
public interface ISpecification<T>
{
    bool Specification IsSatisfiedBy(T item);
}
```

However the example presented was simplified to access a in-memory list containing the objects that are being filtered. If you try to implement it on a real-world application, filtering objects in-memory will hardly be a suitable solution. 
So we need to be able to send the specification to the infrastructure layer. Conventionally, it´ll be a relational database. In the following example I´ll be using *Entity Framework* as the means to access it. However, the example should be quite similar for any ORM. 

> *Vladimir Khorikov* has an excellent [article](http://enterprisecraftsmanship.com/2016/02/08/specification-pattern-c-implementation/) about the specification pattern which I highly recommend. However, his solution adds a new method to the `ISpecification<T>` interface which will probably just be useful for querying the database (although decoupled from it). I believe this violates the *Single Responsibility* principle. Also, he is using the same class of the domain model and on EF [which highly I un-recommend]({{ site.baseurl }}{% post_url 2018-01-12-persistence-Model-not-domain_model %}) on larger systems, making it harder to add behavior and invariants to the entity, thus corrupting the domain model. 

Datasets on the entity framework implement `IQueryable<T>`, meaning it has methods that take an expression tree (such as `IQueryable<T>.Where(Expression<Func<T, bool>>)`) and is able handle those expression in a way other than executing immediately in-memory. On Entity Framework´s case, it can translate the expression tree into a SQL Query to be used against the database.

So, our objective in this article will be to translate an `Specification` class into a SQL Query, while restraining ourselves to SOLID principles, and most importantly, not corrupting the domain model.

Remembering the [previous post]({{ site.baseurl }}{% post_url 2018-01-01-generic-visitor-pattern-in-c %}), whe have created a generic `Visitor`, that could to traverse a specification:

```csharp

//Generic interfaces definitions
public interface ISpecification <in T, in TVisitor>  where TVisitor : ISpecificationVisitor<TVisitor, T>
{
    bool IsSatisfiedBy(T item);
    void Accept (TVisitor visitor);
}

public interface ISpecificationVisitor<TVisitor, T>  where TVisitor : ISpecificationVisitor<TVisitor, T>
{
    void Visit(AndSpecification<T, TVisitor> spec);
    void Visit(OrSpecification<T, TVisitor> spec);
    void Visit(NotSpecification<T, TVisitor> spec);
}

//Specific implementation
public interface IProductSpecificationVisitor : ISpecificationVisitor<IProductSpecificationVisitor, Product>
{
    void Visit (ProductMatchesCategory spec);
    void Visit (ProductPriceInRange spec);
}

public class ProductMatchesCategory : ISpecification<Product, IProductSpecificationVisitor> 
{
    public readonly string Category;

    public ProductMatchesCategory (string category) 
    {
        this.Category = category;
    }

    public bool IsSatisfiedBy (Product item) => item.Category == Category;

    public void Accept (IProductSpecificationVisitor visitor) 
    {
        visitor.Visit (this); 
    }
}
```

As example, consider the following domain model of a product aggregate: 

```csharp
public class Product 
{
    public string ID  { get; }
    public ProductCategory Category { get; } //value-object
    //other properties & methods
}

public class ProductCategory 
{
    public ProductCategory (string name) { CategoryName = name; }
    public string CategoryName { get; }
 }
```

Here, the `ProductCategory` is a [Value Object](https://martinfowler.com/bliki/ValueObject.html), encapsulating a single property (`CategoryName`).
Also, consider the the following persistence model of the same aggregate, which I´ll differentiate with the prefix `EF` (for Entity-Framework)

```csharp
public class EFProduct 
{
    public string ID  { get; set; }
    public string Category { get; set; } // saving as string
    //other properties
}
```
Note that the `Category` on the persistence model can be simplified to a string, bypassing any headache to map the value object to a column on the database.

As you surely know, in order to filter the products on the database, we will need to use the `DbSet`´s `Where` method: 

```csharp
var filteredProducts = db.Products.Where(p => p.Category == "CategoryABC");
```

Looking at the signature of the `Where` method, we realize that the parameter is actually an expression tree. Which EF will translate into a SQL Query:

```csharp
    IQueryable<EFProduct>.Where(Expression<Func<EFProduct, bool>> predicate);
```

Therefore, to use our specification with the database, we need something to convert a `ISpecification<Product>` into a `Expression<Func<EFProduct, bool>>`. We´ll create a Visitor to do just that:

```csharp
public class ProductEFExpressionVisitor : IProductSpecificationVisitor 
{
    public Expression<Func<EFProduct, bool>> Expr { get; protected set; } = null;
    public void Visit (ProductMatchesCategory spec) 
    {
        var categoryName = spec.Category.CategoryName;
        Expr = ef => ef.Category == categoryName;
    }

    //other specification-specific visit methods
}
```

which you will be able to use it like this:

```csharp
public class ProductRepository
{
    public IEnumerable<Product> GetProducts (IProductSpecification spec)
    {
        var visitor = new ProductProductEFExpressionVisitor();
        spec.Accept(visitor);
        var expression = visitor.Expr;

        using (var db = new ProductDBEntities())
        {
            foreach (var efProduct in db.Products.Where(expression))
            {
                var product = ConvertPersistenceToDomain(efProduct);
                yield return product;
            }
        }
    }

    private Product ConvertPersistenceToDomain(EFProduct entity) { /* ... */ }
}
```

So now, our repository has a generic method for querying products given any available filter that we implement as a specification. 

### But Wait! What about the boolean operators ?

The specification pattern without boolean operators is no fun at all. So we need to add methods to the expression visitor to create boolean operators. 

As example, remember that we defined the `And` specification as:

```csharp
public class AndSpecification<T, TVisitor> :ISpecification<T, TVisitor> 
    where TVisitor : ISpecificationVisitor<TVisitor, T>
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

Therefore we can enhance the visitor with this specification:

```csharp
public class ProductEFExpressionVisitor : IProductSpecificationVisitor 
{
    public Expression<Func<EFProduct, bool>> Expr { get; protected set; }

    public void Visit (ProductMatchesCategory spec) 
    {
        var categoryName = spec.Category.CategoryName;
        Expr = ef => ef.Category == categoryName;
    }

    public void Visit (AndSpecification<Product,IProductSpecificationVisitor>  spec) 
    {
        var leftExpr = ConvertSpecToExpression (spec.Left);
        var rightExpr = ConvertSpecToExpression (spec.Right);

        var exprBody = Expression.AndAlso (leftExpr.Body, rightExpr.Body);
        Expr = Expression.Lambda<Func<EFProduct, bool>> (exprBody, leftExpr.Parameters.Single ());
    }

    private Expression<Func<EFProduct, bool>> ConvertSpecToExpression (
                    ISpecification<Product, IProductSpecificationVisitor> spec) 
    {
        var visitor = new ProductEFExpressionVisitor ();
        spec.Accept (visitor);
        return visitor.Expr;
    }    

    //other specification type-specific methods
}
```

This is a little more complex than the previous example, so let´s detail it a little bit better.

The `And` specification has two properties (`Left` and `Right`). Each of which are specifications themselves. The `And` operator is responsible for defining that both conditions are met. In our case, we will need to convert both `Left` and `Right` into expression trees, which is done by the `ConvertSpecToExpression`.

Both results (`leftExpr` and `rightExpr`) are then combined together using the `Expression.AndAlso` helper method from .NET to create a new expression body.

Next, we use the `Expression.Lambda` method to add the parameter back to the expression (single parameter `EFProduct`, which we can just copy it from either `leftExpr` or `rightExpr`)


### Wasn´t the visitor supposed to be generic ?

On a [previous post]({{ site.baseurl }}{% post_url 2018-01-01-generic-visitor-pattern-in-c %}), we created a generic specification so we could reuse the boolean operators (and, or, not). having to implement all boolean operators of every Aggregate visitor doesn´t feel right. Instead, let´s create an abstract class already implementing the expression conversion for the boolean operators.

```csharp
public abstract class EFExpressionVisitor<TEntity, TVisitor, TItem>
                        where TVisitor : ISpecificationVisitor<TVisitor, TItem>
{
    public Expression<Func<TEntity, bool>> Expr { get; protected set; }

    public abstract Expression<Func<TEntity, bool>> ConvertSpecToExpression (ISpecification<TItem, TVisitor> spec);

    public void Visit (AndSpecification<TItem, TVisitor> spec)
    {
        var leftExpr = ConvertSpecToExpression (spec.Left);
        var rightExpr = ConvertSpecToExpression (spec.Right);

        var exprBody = Expression.AndAlso (leftExpr.Body, rightExpr.Body);
        Expr = Expression.Lambda<Func<TEntity, bool>> (exprBody, leftExpr.Parameters.Single ());
    }

    public void Visit (OrSpecification<TItem, TVisitor> spec)
    {
        var leftExpr = ConvertSpecToExpression (spec.Left);
        var rightExpr = ConvertSpecToExpression (spec.Right);

        var exprBody = Expression.Or (leftExpr.Body, rightExpr.Body);
        Expr = Expression.Lambda<Func<TEntity, bool>> (exprBody, leftExpr.Parameters.Single ());
    }

    public void Visit (NotSpecification<TItem, TVisitor> spec)
    {
        var specExpr = ConvertSpecToExpression (spec.Spec);

        var exprBody = Expression.Not (specExpr.Body);
        Expr = Expression.Lambda<Func<TEntity, bool>> (exprBody, specExpr.Parameters.Single ());
    }
}
```
Note that we had to add another generic type restriction `TEntity` which is the aggregate from the persistence model (`EFProduct` in our example).

Also, I´ve left the `ConvertSpecToExpression` method abstract, because we will need to instantiate the visitor from within this method. The `ProductEFExpressionVisitor` now will look like this:

```csharp
public class ProductEFExpressionVisitor 
    : EFExpressionVisitor<EFProduct, IProductSpecificationVisitor, Product>, IProductSpecificationVisitor
{
    public override Expression<Func<EFProduct, bool>> ConvertSpecToExpression (ISpecification<Product, IProductSpecificationVisitor> spec)
    {
        var visitor = new ProductEFExpressionVisitor ();
        spec.Accept (visitor);
        return visitor.Expr;
    }

    public void Visit (ProductMatchesCategory spec)
    {
        var categoryName = spec.Category.CategoryName;
        Expr = ef => ef.Category == categoryName;
    }

    //Other methods for product-specific specifications
}
```

You can then add new specification types for each kind of filter you´re interested in (for example: `ProductNameContains`, or `PriceInRange`, etc). It´s a lot of code, but the usage in the end will be very straight forward, specially if using the extension methods have defined [previously]({{ site.baseurl }}{% post_url 2017-12-18-a-generic-specification-pattern-in-c %})

```csharp
var specification = 
    new ProductPriceInRange(0M, 100M)
        .And (new ProductMatchesCategory("Clothes")
                .Or(new ProductMatchesCategory("Electronics")));

var products = repository.GetProducts(specification);
```
The same idea can be used for other proposes, such as serializing the specification.

> It´s important to note that the `ProductEFExpressionVisitor` class belongs to the infrastructure layer (on a hexagonal architecture), and may even be internal, since it is only used by the repository.

The same idea can be applied if you have a different persistence, or if you need to serialize the specification for example.

This way we´re able to create specifications for our domain model, and translate it to the persistence model. We´ve also managed create a generic approach where all required specifications will (almost) automatically have boolean operators to work with.
