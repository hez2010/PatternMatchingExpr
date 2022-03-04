using PatternMatchingExpr;

{
    Expr<int> a = 4;
    Expr<int> b = -3;
    Expr<int> x = "x";
    Expr<int> c = !((a + b) * (a - b) > x);
    Expr<int> y = "y";
    Expr<int> z = "z";
    Expr<int> expr = (c.Switch(y, z) - a > x).Switch(z + a, y / b);
    Console.WriteLine(expr);
    Console.WriteLine(expr.InteractiveEval());
}

Expr<int> expr1, expr2, expr3;
{
    Expr<int> a = 4;
    Expr<int> b = -3;
    Expr<int> x = "x";
    Expr<int> c = !((a + b) * (a - b) > x);
    Expr<int> y = "y";
    Expr<int> z = "z";
    expr1 = (c.Switch(y, z) - a > x).Switch(z + a, y / b);
}

{
    Expr<int> a = 4;
    Expr<int> b = -3;
    Expr<int> x = "x";
    Expr<int> c = !((a + b) * (a - b) > x);
    Expr<int> y = "y";
    Expr<int> z = "z";
    expr2 = (c.Switch(y, z) - a > x).Switch(z + a, y / b);
}

{
    Expr<int> a = 4;
    Expr<int> b = -3;
    Expr<int> x = "x";
    Expr<int> c = !((a + b) * (a - b) > x);
    Expr<int> y = "y";
    Expr<int> w = "w";
    expr3 = (c.Switch(y, w) - a > x).Switch(w + a, y / b);
}

Console.WriteLine(expr1.Equals(expr2));
Console.WriteLine(expr1.Equals(expr3));