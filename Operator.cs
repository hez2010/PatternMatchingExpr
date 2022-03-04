namespace PatternMatchingExpr;

public abstract record Operator
{
    public record UnaryOperator(Operators Operator) : Operator;
    public record BinaryOperator(Operators Operator) : Operator;
}
