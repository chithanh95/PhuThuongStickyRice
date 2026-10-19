using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace PhuThuongStickyRice.Domain.Specifications
{
    /// <summary>
    /// Specification luôn đúng — phần tử trung hòa của phép AND/OR.
    /// Dùng làm giá trị mặc định khi không có điều kiện lọc.
    /// </summary>
    /// <typeparam name="T">Kiểu entity cần đánh giá.</typeparam>
    internal sealed class IdentitySpecification<T> : Specification<T>
    {
        public override Expression<Func<T, bool>> ToExpression()
        {
            return x => true;
        }
    }

    /// <summary>
    /// Specification pattern (DDD): đóng gói một điều kiện nghiệp vụ thành
    /// đối tượng có thể kết hợp AND/OR/NOT, tái sử dụng ở nhiều query.
    /// Có 2 cách dùng: <see cref="IsSatisfiedBy"/> để kiểm tra trực tiếp trên
    /// entity trong memory, hoặc <see cref="ToExpression"/> để EF dịch thành
    /// điều kiện WHERE trong SQL.
    /// </summary>
    /// <typeparam name="T">Kiểu entity cần đánh giá.</typeparam>
    public abstract class Specification<T>
    {
        /// <summary>Specification luôn đúng — phần tử trung hòa của AND/OR.</summary>
        public static readonly Specification<T> All = new IdentitySpecification<T>();

        /// <summary>
        /// Kiểm tra entity có thỏa điều kiện không (chạy trong memory).
        /// </summary>
        /// <param name="entity">Entity cần đánh giá.</param>
        /// <returns>true nếu thỏa điều kiện.</returns>
        public bool IsSatisfiedBy(T entity)
        {
            Func<T, bool> predicate = ToExpression().Compile();
            return predicate(entity);
        }

        /// <summary>
        /// Trả về biểu thức điều kiện để EF Core dịch thành WHERE trong SQL.
        /// </summary>
        /// <returns>Biểu thức <c>Func&lt;T, bool&gt;</c> của điều kiện.</returns>
        public abstract Expression<Func<T, bool>> ToExpression();

        /// <summary>
        /// Kết hợp 2 specification bằng AND. Nếu một bên là <see cref="All"/>
        /// thì trả về bên còn lại (rút gọn để cây biểu thức không phình to).
        /// </summary>
        /// <param name="specification">Specification cần AND với specification hiện tại.</param>
        /// <returns>Specification mới biểu diễn điều kiện AND.</returns>
        public Specification<T> And(Specification<T> specification)
        {
            if (this == All)
            {
                return specification;
            }

            if (specification == All)
            {
                return this;
            }

            return new AndSpecification<T>(this, specification);
        }

        /// <summary>
        /// Kết hợp 2 specification bằng OR. Nếu một bên là <see cref="All"/>
        /// thì kết quả luôn đúng — trả về <see cref="All"/> luôn.
        /// </summary>
        /// <param name="specification">Specification cần OR với specification hiện tại.</param>
        /// <returns>Specification mới biểu diễn điều kiện OR.</returns>
        public Specification<T> Or(Specification<T> specification)
        {
            if (this == All || specification == All)
            {
                return All;
            }

            return new OrSpecification<T>(this, specification);
        }

        /// <summary>
        /// Đảo ngược điều kiện (NOT).
        /// </summary>
        /// <returns>Specification mới biểu diễn phủ định của điều kiện hiện tại.</returns>
        public Specification<T> Not()
        {
            return new NotSpecification<T>(this);
        }
    }

    /// <summary>
    /// Triển khai AND: ghép 2 biểu thức con bằng <c>Expression.AndAlso</c>.
    /// Dùng parameter của biểu thức trái để 2 vế chia sẻ cùng một tham số x.
    /// </summary>
    /// <typeparam name="T">Kiểu entity cần đánh giá.</typeparam>
    internal sealed class AndSpecification<T> : Specification<T>
    {
        private readonly Specification<T> _left;
        private readonly Specification<T> _right;

        public AndSpecification(Specification<T> left, Specification<T> right)
        {
            _left = left;
            _right = right;
        }

        public override Expression<Func<T, bool>> ToExpression()
        {
            Expression<Func<T, bool>> leftExpression = _left.ToExpression();
            Expression<Func<T, bool>> rightExpression = _right.ToExpression();

            BinaryExpression andExpression = Expression.AndAlso(leftExpression.Body, rightExpression.Body);

            return Expression.Lambda<Func<T, bool>>(andExpression, leftExpression.Parameters.Single());
        }
    }

    /// <summary>
    /// Triển khai OR: ghép 2 biểu thức con bằng <c>Expression.OrElse</c>.
    /// Dùng parameter của biểu thức trái để 2 vế chia sẻ cùng một tham số x.
    /// </summary>
    /// <typeparam name="T">Kiểu entity cần đánh giá.</typeparam>
    internal sealed class OrSpecification<T> : Specification<T>
    {
        private readonly Specification<T> _left;
        private readonly Specification<T> _right;

        public OrSpecification(Specification<T> left, Specification<T> right)
        {
            _left = left;
            _right = right;
        }

        public override Expression<Func<T, bool>> ToExpression()
        {
            Expression<Func<T, bool>> leftExpression = _left.ToExpression();
            Expression<Func<T, bool>> rightExpression = _right.ToExpression();

            BinaryExpression orExpression = Expression.OrElse(leftExpression.Body, rightExpression.Body);

            return Expression.Lambda<Func<T, bool>>(orExpression, leftExpression.Parameters.Single());
        }
    }

    /// <summary>
    /// Triển khai NOT: phủ định biểu thức con bằng <c>Expression.Not</c>.
    /// </summary>
    /// <typeparam name="T">Kiểu entity cần đánh giá.</typeparam>
    internal sealed class NotSpecification<T> : Specification<T>
    {
        private readonly Specification<T> _specification;

        public NotSpecification(Specification<T> specification)
        {
            _specification = specification;
        }

        public override Expression<Func<T, bool>> ToExpression()
        {
            Expression<Func<T, bool>> expression = _specification.ToExpression();
            UnaryExpression notExpression = Expression.Not(expression.Body);

            return Expression.Lambda<Func<T, bool>>(notExpression, expression.Parameters.Single());
        }
    }
}
