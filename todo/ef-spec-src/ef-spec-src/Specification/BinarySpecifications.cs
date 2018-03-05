namespace Marreco.SpecificationBlog.Specifications {

    public class AndSpecification<T, TVisitor> : ISpecification<T, TVisitor> where TVisitor : ISpecificationVisitor<TVisitor, T> {
        public ISpecification<T, TVisitor> Left { get; }
        public ISpecification<T, TVisitor> Right { get; }

        public AndSpecification (ISpecification<T, TVisitor> left, ISpecification<T, TVisitor> right) {
            this.Left = left;
            this.Right = right;
        }

        public void Accept (TVisitor visitor) => visitor.Visit (this);
        public bool IsSatisfiedBy (T obj) => Left.IsSatisfiedBy (obj) && Right.IsSatisfiedBy (obj);
    }

    public class OrSpecification<T, TVisitor> : ISpecification<T, TVisitor> where TVisitor : ISpecificationVisitor<TVisitor, T> {
        public ISpecification<T, TVisitor> Left { get; }
        public ISpecification<T, TVisitor> Right { get; }

        public OrSpecification (ISpecification<T, TVisitor> left, ISpecification<T, TVisitor> right) {
            this.Left = left;
            this.Right = right;
        }

        public void Accept (TVisitor visitor) => visitor.Visit (this);
        public bool IsSatisfiedBy (T obj) => Left.IsSatisfiedBy (obj) || Right.IsSatisfiedBy (obj);
    }

    public class NotSpecification<T, TVisitor> : ISpecification<T, TVisitor> where TVisitor : ISpecificationVisitor<TVisitor, T> {
        public ISpecification<T, TVisitor> Spec { get; }

        public NotSpecification (ISpecification<T, TVisitor> spec) {
            this.Spec = spec;
        }

        public void Accept (TVisitor visitor) => visitor.Visit (this);
        public bool IsSatisfiedBy (T obj) => !Spec.IsSatisfiedBy (obj);
    }
}