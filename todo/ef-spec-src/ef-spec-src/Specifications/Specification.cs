using System;

namespace Marreco.SpecificationBlog.Specifications {

    public interface ISpecification<in T, in TVisitor> where TVisitor : ISpecificationVisitor<TVisitor, T> {
        bool IsSatisfiedBy (T item);
        void Accept (TVisitor visitor);
    }

}