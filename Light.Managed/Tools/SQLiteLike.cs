using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Light.Managed.Tools
{
    public class CustomSqliteCompositeMethodCallTranslator : SqliteCompositeMethodCallTranslator
    {
        private static readonly IMethodCallTranslator[] _methodCallTranslators =
        {
            new SqliteLikeTranslator()
        };

        public CustomSqliteCompositeMethodCallTranslator(ILogger<SqliteCompositeMethodCallTranslator> logger) : base(logger)
        {
            AddTranslators(_methodCallTranslators);
        }
    }

    public class SqliteLikeTranslator : IMethodCallTranslator
    {
        private static readonly MethodInfo _methodInfo = typeof(SqliteExtensions)
            .GetRuntimeMethod(nameof(SqliteExtensions.Like), new[] { typeof(string) , typeof(string) });

        private static readonly MethodInfo _concat = typeof(string)
            .GetRuntimeMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) });

        public Expression Translate(MethodCallExpression methodCallExpression)
        {
            return ReferenceEquals(methodCallExpression.Method, _methodInfo)
                ? new LikeExpression(
                    methodCallExpression.Arguments[0],
                    Expression.Add(
                        Expression.Add(
                            Expression.Constant("%", typeof(string)), methodCallExpression.Arguments[1], _concat),
                            Expression.Constant("%", typeof(string)),
                        _concat))
                    : null;
        }
    }

    public static class SqliteExtensions
    {
        public static bool Like(this string str, string match)
        {
            return str.IndexOf(match, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }
    }
}
