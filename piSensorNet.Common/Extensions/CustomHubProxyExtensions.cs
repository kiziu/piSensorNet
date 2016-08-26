using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Extensions;

namespace Microsoft.AspNet.SignalR.Client
{
    public static class CustomHubProxyExtensions
    {
        public static Task SafeInvoke(this IHubProxy proxy, string methodName, params object[] args)
            => proxy.Invoke(methodName, args.MapArray(i => i ?? Null.Value));
        
        public static Task SafeInvoke<TInterface>(this IHubProxy proxy, Expression<Action<TInterface>> expression)
        {
            var methodCall = (MethodCallExpression)expression.Body;
            var method = methodCall.Method;
            var methodArguments = methodCall.Arguments.ReadOnly();
            var args = methodArguments.MapArray(ExtractValue);

            return proxy.Invoke(method.Name, args);
        }

        private static object ExtractValue(Expression expression)
        {
            var memberExpression = expression as MemberExpression;
            if (memberExpression != null)
            {
                var member = memberExpression.Member;
                var constant = memberExpression.Expression as ConstantExpression;

                if (constant != null)
                    return member.GetMemberValue(constant.Value);
            }

            throw new ArgumentOutOfRangeException(nameof(expression), expression, "Unsupported type of expression.");
        }
    }
}