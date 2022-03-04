using System.ComponentModel;

namespace PatternMatchingExpr;

public enum Operators
{
    [Description("~")] Inv, [Description("-")] Min, [Description("!")] LogicalNot,
    [Description("+")] Add, [Description("-")] Sub, [Description("*")] Mul, [Description("/")] Div,
    [Description("&")] And, [Description("|")] Or, [Description("^")] Xor,
    [Description("==")] Eq, [Description("!=")] Ne,
    [Description(">")] Gt, [Description("<")] Lt, [Description(">=")] Ge, [Description("<=")] Le,
    [Description("&&")] LogicalAnd, [Description("||")] LogicalOr,
}
