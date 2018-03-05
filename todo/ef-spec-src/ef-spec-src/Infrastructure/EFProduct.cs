using System;
using System.Linq;
using System.Linq.Expressions;
using Marreco.SpecificationBlog.Specifications;

namespace Marreco.SpecificationBlog {

    public class EFProduct {
        public string Category;
    }

    public class ProductEFExpressionVisitor : IProductSpecificationVisitor {
        public Expression<Func<EFProduct, bool>> Expr { get; private set; }

        private ProductEFExpressionVisitor
            () { }

        public static Expression<Func<EFProduct, bool>> ExpressionForSpecification (ISpecification<Product, IProductSpecificationVisitor> spec) {
            var visitor = new ProductEFExpressionVisitor ();
            spec.Accept (visitor);
            return visitor.Expr;
        }

        public void Visit (ProductMatchesCategory spec) => Expr = ef => ef.Category == spec.Category;

        public void Visit (AndSpecification<Product, IProductSpecificationVisitor> spec) {
            var leftExpr = ExpressionForSpecification (spec.Left);
            var rightExpr = ExpressionForSpecification (spec.Right);

            var exprBody = Expression.AndAlso (leftExpr.Body, rightExpr.Body);
            Expr = Expression.Lambda<Func<EFProduct, bool>> (exprBody, leftExpr.Parameters.Single ());
        }

        public void Visit (OrSpecification<Product, IProductSpecificationVisitor> spec) {
            var leftExpr = ExpressionForSpecification (spec.Left);
            var rightExpr = ExpressionForSpecification (spec.Right);

            var exprBody = Expression.Or (leftExpr.Body, rightExpr.Body);
            Expr = Expression.Lambda<Func<EFProduct, bool>> (exprBody, leftExpr.Parameters.Single ());
        }

        public void Visit (NotSpecification<Product, IProductSpecificationVisitor> spec) {
            var specExpr = ExpressionForSpecification (spec.Spec);

            var exprBody = Expression.Not (specExpr.Body);
            Expr = Expression.Lambda<Func<EFProduct, bool>> (exprBody, specExpr.Parameters.Single ());
        }
    }

}