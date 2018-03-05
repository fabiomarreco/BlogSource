using System;
using Marreco.SpecificationBlog.Infrastructure;
using Marreco.SpecificationBlog.Products;
using Marreco.SpecificationBlog.Specifications;

namespace Marreco.SpecificationBlog {
    class Program {
        static void Main (string[] args) {

            var productSpec =
                new ProductMatchesCategory ("cat1").Or (new ProductMatchesCategory ("cat2"));

            var expr = new ProductEFExpressionVisitor().ExpressionForSpecification (productSpec);

            Console.WriteLine (expr.ToString ());

            var fn = expr.Compile ();

            Console.WriteLine (fn.Invoke (new EFProduct () { Category = "cat3" }));
            Console.WriteLine (fn.Invoke (new EFProduct () { Category = "cat1" }));
            Console.WriteLine (fn.Invoke (new EFProduct () { Category = "cat2" }));

        }

    }
}