using System;
using System.Linq;
using System.Linq.Expressions;
using Marreco.SpecificationBlog.Products;
using Marreco.SpecificationBlog.Specifications;

namespace Marreco.SpecificationBlog.Infrastructure {
    public abstract class EFExpressionVisitor<TEntity, TVisitor, TItem>
        where TVisitor : ISpecificationVisitor<TVisitor, TItem> {
            public Expression<Func<TEntity, bool>> Expr { get; protected set; }

            public abstract Expression<Func<TEntity, bool>> ExpressionForSpecification (ISpecification<TItem, TVisitor> spec);

            public void Visit (AndSpecification<TItem, TVisitor> spec) {
                var leftExpr = ExpressionForSpecification (spec.Left);
                var rightExpr = ExpressionForSpecification (spec.Right);

                var exprBody = Expression.AndAlso (leftExpr.Body, rightExpr.Body);
                Expr = Expression.Lambda<Func<TEntity, bool>> (exprBody, leftExpr.Parameters.Single ());
            }

            public void Visit (OrSpecification<TItem, TVisitor> spec) {
                var leftExpr = ExpressionForSpecification (spec.Left);
                var rightExpr = ExpressionForSpecification (spec.Right);

                var exprBody = Expression.Or (leftExpr.Body, rightExpr.Body);
                Expr = Expression.Lambda<Func<TEntity, bool>> (exprBody, leftExpr.Parameters.Single ());
            }

            public void Visit (NotSpecification<TItem, TVisitor> spec) {
                var specExpr = ExpressionForSpecification (spec.Spec);

                var exprBody = Expression.Not (specExpr.Body);
                Expr = Expression.Lambda<Func<TEntity, bool>> (exprBody, specExpr.Parameters.Single ());
            }
        }

}