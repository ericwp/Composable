#region usings

using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;

#endregion

namespace Composable.System.Linq
{
    public static class ExpressionUtil
    {
        public static string ExtractMemberName<TValue>(Expression<Func<TValue>> func)
        {
            return ExtractMemberName((LambdaExpression)func);
        }

        public static string ExtractMemberName<TParam, TValue>(Expression<Func<TParam, TValue>> func)
        {
            return ExtractMemberName((LambdaExpression)func);
        }

        public static string ExtractMemberName<TParam, TParam2, TValue>(Expression<Func<TParam, TParam2, TValue>> func)
        {
            return ExtractMemberName((LambdaExpression)func);
        }

        public static string ExtractMemberName(LambdaExpression lambda)
        {
            Contract.Requires(lambda != null);
            var body = lambda.Body;
            MemberExpression memberExpression;

            if(body is UnaryExpression)
            {
                memberExpression = (MemberExpression)((UnaryExpression)body).Operand;
            }
            else
            {
                memberExpression = (MemberExpression)body;
            }

            return memberExpression.Member.Name;
        }
    }
}