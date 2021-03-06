using System;
using System.Linq.Expressions;
using Composable.Contracts;

namespace Composable.System.Linq
{
    ///<summary>Extracts member names from expressions</summary>
    static class ExpressionUtil
    {
        public static string ExtractMethodName(Expression<Action> func)
        {
            Contract.Argument(() => func).NotNull();
            return ((MethodCallExpression)func.Body).Method.Name;
        }

        ///<summary>Extracts the name of the member that the supplied func expression returns.</summary>
        public static string ExtractMethodName<T>(Expression<Func<T>> func)
        {
            Contract.Argument(() => func).NotNull();
            return ((MethodCallExpression)func.Body).Method.Name;
        }

        ///<summary>Extracts the name of the member that the supplied func expression returns.</summary>
        public static string ExtractMemberName<TValue>(Expression<Func<TValue>> func)
        {
            Contract.Argument(() => func).NotNull();
            return ExtractMemberName((LambdaExpression)func);
        }

        ///<summary>Extracts the name of the member that the supplied func expression returns.</summary>
        public static string ExtractMemberName<TParam, TValue>(Expression<Func<TParam, TValue>> func)
        {
            Contract.Argument(() => func).NotNull();
            return ExtractMemberName((LambdaExpression)func);
        }

        ///<summary>Extracts the name of the member that the supplied func expression returns.</summary>
        public static string ExtractMemberName<TParam, TParam2, TValue>(Expression<Func<TParam, TParam2, TValue>> func)
        {
            Contract.Argument(() => func).NotNull();
            return ExtractMemberName((LambdaExpression)func);
        }

        ///<summary>Extracts the name of the member that the supplied lambda expression returns.</summary>
        static string ExtractMemberName(LambdaExpression lambda)
        {
            Contract.Argument(() => lambda).NotNull();

            var memberExpression = lambda.Body is UnaryExpression unaryExpression
                                       ? (MemberExpression)unaryExpression.Operand
                                       : (MemberExpression)lambda.Body;

            return memberExpression.Member.Name;
        }

        public static string ExtractMemberPath<TValue>(Expression<Func<TValue>> func)
        {
            Contract.Argument(() => func).NotNull();
            return ExtractMemberPath((LambdaExpression)func);
        }

        static string ExtractMemberPath(LambdaExpression lambda)
        {
            Contract.Argument(() => lambda).NotNull();
            var memberExpression = lambda.Body is UnaryExpression unaryExpression
                                       ? (MemberExpression)unaryExpression.Operand
                                       : (MemberExpression)lambda.Body;

            // ReSharper disable once PossibleNullReferenceException
            return $"{memberExpression.Member.DeclaringType.FullName}.{memberExpression.Member.Name}";
        }
    }
}
