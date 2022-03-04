using System.ComponentModel;
using System.Reflection;

namespace PatternMatchingExpr;

public static class Extensions
{
    public static Expr<T> Switch<T>(this Expr<T> cond, Expr<T> left, Expr<T> right) where T : IBinaryNumber<T> => new Expr<T>.TernaryExpr(cond, left, right);
    public static string? GetName<T>(this T op) where T : Enum => typeof(T).GetMember(op.ToString()).FirstOrDefault()?.GetCustomAttribute<DescriptionAttribute>()?.Description;
}
