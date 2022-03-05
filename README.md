# C# 模式匹配完全指南
## 前言
自从 2017 年 C# 7.0 版本开始引入声明模式和常数模式匹配开始，到 2022 年的 C# 11 为止，最后一个板块列表模式和切片模式匹配也已经补齐，当初计划的模式匹配内容已经基本全部完成。

C# 在模式匹配方面下一步计划则是支持活动模式（active pattern），这一部分将在本文最后进行介绍，而在介绍未来的模式匹配计划之前，本文主题是对截止 C# 11 模式匹配的<del>（不）</del>完全指南，希望能对各位开发者们提升代码编写效率、可读性和质量有所帮助。

## 模式匹配
要使用模式匹配，首先要了解什么是模式。在使用正则表达式匹配字符串时，正则表达式自己就是一个模式，而对字符串使用这段正则表达式进行匹配的过程就是模式匹配。而在代码中也是同样的，我们对对象采用某种模式进行匹配的过程就是模式匹配。

C# 11 支持的模式有很多，包含：

- 声明模式（declaration pattern）
- 类型模式（type pattern）
- 常数模式（constant pattern）
- 关系模式（relational pattern）
- 逻辑模式（logical pattern）
- 属性模式（property pattern）
- 位置模式（positional pattern）
- var 模式（var pattern）
- 丢弃模式（discard pattern）
- 列表模式（list pattern）
- 切片模式（slice pattern）

而其中，不少模式都支持递归，也就意味着可以模式嵌套模式，以此来实现更加强大的匹配功能。

如果你不清楚这些模式的话，可以访问 https://docs.microsoft.com/zh-cn/dotnet/csharp/language-reference/operators/patterns 进行了解。

模式匹配可以通过 `switch` 表达式来使用，也可以在普通的 `switch` 语句中作为 `case` 使用，还可以在 `if` 条件中通过 `is` 来使用。本文主要在 `switch` 表达式中使用模式匹配。

那么接下来就对这些模式进行介绍。

## 实例：表达式计算器

为了更直观地介绍模式匹配，我们接下来利用模式匹配来编写一个表达式计算器。

为了编写表达式计算器，首先我们需要对表达式进行抽象：

```csharp
public abstract partial class Expr<T> where T : IBinaryNumber<T>
{
    public abstract T Eval(params (string Name, T Value)[] args);
}
```

我们用上面这个 `Expr<T>` 来表示一个表达式，其中 `T` 是操作数的类型，然后进一步将表达式分为常数表达式 `ConstantExpr`、参数表达式 `ParameterExpr`、一元表达式 `UnaryExpr`、二元表达式 `BinaryExpr` 和三元表达式 `TernaryExpr`。最后提供一个 `Eval` 方法，用来计算表达式的值，该方法可以传入一个 `args` 来提供表达式计算所需要的参数。

有了一、二元表达式自然也需要运算符，例如加减乘除等，我们也同时定义 `Operator` 来表示运算符：

```csharp
public abstract record Operator
{
    public record UnaryOperator(Operators Operator) : Operator;
    public record BinaryOperator(Operators Operator) : Operator;
}
```

然后设置允许的运算符，其中前三个是一元运算符，后面的是二元运算符：

```csharp
public enum Operators
{
    [Description("~")] Inv, [Description("-")] Min, [Description("!")] LogicalNot,
    [Description("+")] Add, [Description("-")] Sub, [Description("*")] Mul, [Description("/")] Div,
    [Description("&")] And, [Description("|")] Or, [Description("^")] Xor,
    [Description("==")] Eq, [Description("!=")] Ne,
    [Description(">")] Gt, [Description("<")] Lt, [Description(">=")] Ge, [Description("<=")] Le,
    [Description("&&")] LogicalAnd, [Description("||")] LogicalOr,
}
```

你可以能会好奇对 `T` 的运算能如何实现逻辑与或非，关于这一点，我们直接使用 `0` 来代表 `false`，非 `0` 代表 `true`。

接下来就是分别实现各类表达式的时间！

### 常数表达式
常数表达式很简单，它保存一个常数值，因此只需要在构造方法中将用户提供的值存储下来。它的 `Eval` 实现也只需要简单返回存储的值即可：

```csharp
public abstract partial class Expr<T> where T : IBinaryNumber<T>
{
    public class ConstantExpr : Expr<T>
    {
        public ConstantExpr(T value) => Value = value;

        public T Value { get; }
        public void Deconstruct(out T value) => value = Value;

        public override T Eval(params (string Name, T Value)[] args) => Value;
    }
}
```

### 参数表达式
参数表达式用来定义表达式计算过程中的参数，允许用户在对表达式执行 `Eval` 计算结果的时候传参，因此只需要存储参数名。它的 `Eval` 实现需要根据参数名在 `args` 中找出对应的参数值：

```csharp
public abstract partial class Expr<T> where T : IBinaryNumber<T>
{
    public class ParameterExpr : Expr<T>
    {
        public ParameterExpr(string name) => Name = name;

        public string Name { get; }
        public void Deconstruct(out string name) => name = Name;

        // 对 args 进行模式匹配
        public override T Eval(params (string Name, T Value)[] args) => args switch
        {
            // 如果 args 有至少一个元素，那我们把第一个元素拿出来存为 (name, value)，
            // 然后判断 name 是否和本参数表达式中存储的参数名 Name 相同。
            // 如果相同则返回 value，否则用 args 除去第一个元素剩下的参数继续匹配。
            [var (name, value), .. var tail] => name == Name ? value : Eval(tail),
            // 如果 args 是空列表，则说明在 args 中没有找到名字和 Name 相同的参数，抛出异常
            [] => throw new InvalidOperationException($"Expected an argument named {Name}.")
        };
    }
}
```

模式匹配会从上往下依次进行匹配，直到匹配成功为止。

上面的代码中你可能会好奇 `[var (name, value), .. var tail]` 是个什么模式，这个模式整体看是列表模式，并且列表模式内组合使用声明模式、位置模式和切片模式。例如：

- `[]`：匹配一个空列表。
- `[1, _, 3]`：匹配一个长度是 3，并且首尾元素分别是 1、3 的列表。其中 `_` 是丢弃模式，表示任意元素。
- `[_, .., 3]`：匹配一个末元素是 3，并且 3 不是首元素的列表。其中 `..` 是切片模式，表示任意切片。
- `[1, ..var tail]`：匹配一个首元素是 1 的列表，并且将除了首元素之外元素的切片赋值给 `tail`。其中 `var tail` 是声明模式，用于将匹配结果赋值给变量。
- `[var head, ..var tail]`：匹配一个列表，将它第一个元素赋值给 `head`，剩下元素的切片赋值给 `tail`，这个切片里可以没有元素。
- `[var (name, value), ..var tail]`：匹配一个列表，将它第一个元素赋值给 `(name, value)`，剩下元素的切片赋值给 `tail`，这个切片里可以没有元素。其中 `(name, value)` 是位置模式，用于将第一个元素的解构结果根据位置分别赋值给 `name` 和 `value`，也可以写成 `(var name, var value)`。

### 一元表达式
一元表达式用来处理只有一个操作数的计算，例如非、取反等。

```csharp
public abstract partial class Expr<T> where T : IBinaryNumber<T>
{
    public class UnaryExpr : Expr<T>
    {
        public UnaryExpr(UnaryOperator op, Expr<T> expr) => (Op, Expr) = (op, expr);

        public UnaryOperator Op { get; }
        public Expr<T> Expr { get; }
        public void Deconstruct(out UnaryOperator op, out Expr<T> expr) => (op, expr) = (Op, Expr);

        // 对 Op 进行模式匹配
        public override T Eval(params (string Name, T Value)[] args) => Op switch
        {
            // 如果 Op 是 UnaryOperator，则将其解构结果赋值给 op，然后对 op 进行匹配，op 是一个枚举，而 .NET 中的枚举值都是整数
            UnaryOperator(var op) => op switch
            {
                // 如果 op 是 Operators.Inv
                Operators.Inv => ~Expr.Eval(args),
                // 如果 op 是 Operators.Min
                Operators.Min => -Expr.Eval(args),
                // 如果 op 是 Operators.LogicalNot
                Operators.LogicalNot => Expr.Eval(args) == T.Zero ? T.One : T.Zero,
                // 如果 op 的值大于 LogicalNot 或者小于 0，表示不是一元运算符
                > Operators.LogicalNot or < 0 => throw new InvalidOperationException($"Expected an unary operator, but got {op}.")
            },
            // 如果 Op 不是 UnaryOperator
            _ => throw new InvalidOperationException("Expected an unary operator.")
        };
    }
}
```

上面的代码中，首先利用了 C# 元组可作为左值的特性，分别使用一行代码就做完了构造方法和解构方法的赋值：`(Op, Expr) = (op, expr)` 和 `(op, expr) = (Op, Expr)`。如果你好奇能否利用这个特性交换多个变量，答案是可以！

在 `Eval` 中，首先将类型模式、位置模式和声明模式组合成 `UnaryOperator(var op)`，表示匹配 `UnaryOperator` 类型、并且能解构出一个元素的东西，如果匹配则将解构出来的那个元素赋值给 `op`。

然后我们接着对解构出来的 `op` 进行匹配，这里用到了常数模式，例如 `Operators.Inv` 用来匹配 `op` 是否是 `Operators.Inv`。常数模式可以使用各种常数对对象进行匹配。

这里的 `> Operators.LogicalNot` 和 `< 0` 则是关系模式，分别用于匹配大于 `Operators.LogicalNot` 的值和小于 `0` 的指。然后利用逻辑模式 `or` 将两个模式组合起来表示或的关系。逻辑模式除了 `or` 之外还有 `and` 和 `not`。

由于我们在上面穷举了枚举中所有的一元运算符，因此也可以将 `> Operators.LogicalNot or < 0` 换成丢弃模式 `_` 或者 var 模式 `var foo`，两者都用来匹配任意的东西，只不过前者匹配到后直接丢弃，而后者声明了个变量 `foo` 将匹配到的值放到里面：

```csharp
op switch
{
    // ...
    _ => throw new InvalidOperationException($"Expected an unary operator, but got {op}.")
}
```

或

```csharp
op switch
{
    // ...
    var foo => throw new InvalidOperationException($"Expected an unary operator, but got {foo}.")
}
```

### 二元表达式
二元表达式用来表示操作数有两个的表达式。有了一元表达式的编写经验，二元表达式如法炮制即可。

```csharp
public abstract partial class Expr<T> where T : IBinaryNumber<T>
{
    public class BinaryExpr : Expr<T>
    {
        public BinaryExpr(BinaryOperator op, Expr<T> left, Expr<T> right) => (Op, Left, Right) = (op, left, right);

        public BinaryOperator Op { get; }
        public Expr<T> Left { get; }
        public Expr<T> Right { get; }
        public void Deconstruct(out BinaryOperator op, out Expr<T> left, out Expr<T> right) => (op, left, right) = (Op, Left, Right);

        public override T Eval(params (string Name, T Value)[] args) => Op switch
        {
            BinaryOperator(var op) => op switch
            {
                Operators.Add => Left.Eval(args) + Right.Eval(args),
                Operators.Sub => Left.Eval(args) - Right.Eval(args),
                Operators.Mul => Left.Eval(args) * Right.Eval(args),
                Operators.Div => Left.Eval(args) / Right.Eval(args),
                Operators.And => Left.Eval(args) & Right.Eval(args),
                Operators.Or => Left.Eval(args) | Right.Eval(args),
                Operators.Xor => Left.Eval(args) ^ Right.Eval(args),
                Operators.Eq => Left.Eval(args) == Right.Eval(args) ? T.One : T.Zero,
                Operators.Ne => Left.Eval(args) != Right.Eval(args) ? T.One : T.Zero,
                Operators.Gt => Left.Eval(args) > Right.Eval(args) ? T.One : T.Zero,
                Operators.Lt => Left.Eval(args) < Right.Eval(args) ? T.One : T.Zero,
                Operators.Ge => Left.Eval(args) >= Right.Eval(args) ? T.One : T.Zero,
                Operators.Le => Left.Eval(args) <= Right.Eval(args) ? T.One : T.Zero,
                Operators.LogicalAnd => Left.Eval(args) == T.Zero || Right.Eval(args) == T.Zero ? T.Zero : T.One,
                Operators.LogicalOr => Left.Eval(args) == T.Zero && Right.Eval(args) == T.Zero ? T.Zero : T.One,
                < Operators.Add or > Operators.LogicalOr => throw new InvalidOperationException($"Unexpected a binary operator, but got {op}.")
            },
            _ => throw new InvalidOperationException("Unexpected a binary operator.")
        };
    }
}
```

同理，也可以将 `< Operators.Add or > Operators.LogicalOr` 换成丢弃模式或者 var 模式。

### 三元表达式
三元表达式包含三个操作数：条件表达式 `Cond`、为真的表达式 `Left`、为假的表达式 `Right`。该表达式中会根据 `Cond` 是否为真来选择取 `Left` 还是 `Right`，实现起来较为简单：

```csharp
public abstract partial class Expr<T> where T : IBinaryNumber<T>
{
    public class TernaryExpr : Expr<T>
    {
        public TernaryExpr(Expr<T> cond, Expr<T> left, Expr<T> right) => (Cond, Left, Right) = (cond, left, right);

        public Expr<T> Cond { get; }
        public Expr<T> Left { get; }
        public Expr<T> Right { get; }
        public void Deconstruct(out Expr<T> cond, out Expr<T> left, out Expr<T> right) => (cond, left, right) = (Cond, Left, Right);

        public override T Eval(params (string Name, T Value)[] args) => Cond.Eval(args) == T.Zero ? Right.Eval(args) : Left.Eval(args);
    }
}
```

完成。我们用了仅仅几十行代码就完成了全部的核心逻辑！这便是模式匹配的强大之处：简洁、直观且高效。

### 表达式判等
至此为止，我们已经完成了所有的表达式构造、解构和计算的实现。接下来我们为每一个表达式实现判等逻辑，即判断两个表达式（字面上）是否相同。

例如 `a == b ? 2 : 4` 和 `a == b ? 2 : 5` 不相同，`a == b ? 2 : 4` 和 `c == d ? 2 : 4` 不相同，而 `a == b ? 2 : 4` 和 `a == b ? 2 : 4` 相同。

为了实现该功能，我们重写每一个表达式的 `Equals` 和 `GetHashCode` 方法。

#### 常数表达式
常数表达式判等只需要判断常数值是否相等即可：

```csharp
public override bool Equals(object? obj) => obj is ConstantExpr(var value) && value == Value;
public override int GetHashCode() => Value.GetHashCode();
```

#### 参数表达式
参数表达式判等只需要判断参数名是否相等即可：

```csharp
public override bool Equals(object? obj) => obj is ParameterExpr(var name) && name == Name;
public override int GetHashCode() => Name.GetHashCode();
```

#### 一元表达式
一元表达式判等，需要判断被比较的表达式是否是一元表达式，如果也是的话则判断运算符和操作数是否相等：

```csharp
public override bool Equals(object? obj) => obj is UnaryExpr({ Operator: var op }, var expr) && (op, expr).Equals((Op.Operator, Expr));
public override int GetHashCode() => (Op, Expr).GetHashCode();
```

上面的代码中用到了属性模式 `{ Operator: var op }`，用来匹配属性的值，这里直接组合了声明模式将属性 `Operator` 的值赋值给了 `expr`。另外，C# 中的元组可以组合起来进行判等操作，因此不需要写 `op.Equals(Op.Operator) && expr.Equals(Expr)`，而是可以直接写 `(op, expr).Equals((Op.Operator, Expr))`。

#### 二元表达式
和一元表达式差不多，区别在于这次多了一个操作数：

```csharp
public override bool Equals(object? obj) => obj is BinaryExpr({ Operator: var op }, var left, var right) && (op, left, right).Equals((Op.Operator, Left, Right));
public override int GetHashCode() => (Op, Left, Right).GetHashCode();
```

#### 三元表达式
和二元表达式差不多，只不过运算符 `Op` 变成了操作数 `Cond`：

```csharp
public override bool Equals(object? obj) => obj is TernaryExpr(var cond, var left, var right) && cond.Equals(Cond) && left.Equals(Left) && right.Equals(Right);
public override int GetHashCode() => (Cond, Left, Right).GetHashCode();
```

到此为止，我们为所有的表达式都实现了判等。

### 一些工具方法
我们重载一些 `Expr<T>` 的运算符方便我们使用：

```csharp
public static Expr<T> operator ~(Expr<T> operand) => new UnaryExpr(new(Operators.Inv), operand);
public static Expr<T> operator !(Expr<T> operand) => new UnaryExpr(new(Operators.LogicalNot), operand);
public static Expr<T> operator -(Expr<T> operand) => new UnaryExpr(new(Operators.Min), operand);
public static Expr<T> operator +(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Add), left, right);
public static Expr<T> operator -(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Sub), left, right);
public static Expr<T> operator *(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Mul), left, right);
public static Expr<T> operator /(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Div), left, right);
public static Expr<T> operator &(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.And), left, right);
public static Expr<T> operator |(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Or), left, right);
public static Expr<T> operator ^(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Xor), left, right);
public static Expr<T> operator >(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Gt), left, right);
public static Expr<T> operator <(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Lt), left, right);
public static Expr<T> operator >=(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Ge), left, right);
public static Expr<T> operator <=(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Le), left, right);
public static Expr<T> operator ==(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Eq), left, right);
public static Expr<T> operator !=(Expr<T> left, Expr<T> right) => new BinaryExpr(new(Operators.Ne), left, right);
public static implicit operator Expr<T>(T value) => new ConstantExpr(value);
public static implicit operator Expr<T>(string name) => new ParameterExpr(name);
public static implicit operator Expr<T>(bool value) => new ConstantExpr(value ? T.One : T.Zero);

public override bool Equals(object? obj) => base.Equals(obj);
public override int GetHashCode() => base.GetHashCode();
```

由于重载了 `==` 和 `!=`，编译器为了保险起见提示我们重写 `Equals` 和 `GetHashCode`，这里实际上并不需要重写，因此直接调用 `base` 上的方法保持默认行为即可。

然后编写两个扩展方法用来方便构造三元表达式，和从 `Description` 中获取运算符的名字：

```csharp
public static class Extensions
{
    public static Expr<T> Switch<T>(this Expr<T> cond, Expr<T> left, Expr<T> right) where T : IBinaryNumber<T> => new Expr<T>.TernaryExpr(cond, left, right);
    public static string? GetName<T>(this T op) where T : Enum => typeof(T).GetMember(op.ToString()).FirstOrDefault()?.GetCustomAttribute<DescriptionAttribute>()?.Description;
}
```

由于有参数表达式参与时需要我们提前提供参数值才能调用 `Eval` 进行计算，因此我们写一个交互式的 `Eval` 来在计算过程中遇到参数表达式时提示用户输入值，起名叫做 `InteractiveEval`：

```csharp
public T InteractiveEval()
{
    var names = Array.Empty<string>();
    return Eval(GetArgs(this, ref names, ref names));
}
private static T GetArg(string name, ref string[] names)
{
    Console.Write($"Parameter {name}: ");
    string? str;
    do { str = Console.ReadLine(); }
    while (str is null);
    names = names.Append(name).ToArray();
    return T.Parse(str, NumberStyles.Number, null);
}
private static (string Name, T Value)[] GetArgs(Expr<T> expr, ref string[] assigned, ref string[] filter) => expr switch
{
    TernaryExpr(var cond, var left, var right) => GetArgs(cond, ref assigned, ref assigned).Concat(GetArgs(left, ref assigned,ref assigned)).Concat(GetArgs(right, ref assigned, ref assigned)).ToArray(),
    BinaryExpr(_, var left, var right) => GetArgs(left, ref assigned, ref assigned).Concat(GetArgs(right, ref assigned, refassigned)).ToArray(),
    UnaryExpr(_, var uexpr) => GetArgs(uexpr, ref assigned, ref assigned),
    ParameterExpr(var name) => filter switch
    {
        [var head, ..] when head == name => Array.Empty<(string Name, T Value)>(),
        [_, .. var tail] => GetArgs(expr, ref assigned, ref tail),
        [] => new[] { (name, GetArg(name, ref assigned)) }
    },
    _ => Array.Empty<(string Name, T Value)>()
};
```

这里在 `GetArgs` 方法中，模式 `[var head, ..]` 后面跟了一个 `when head == name`，这里的 `when` 用来给模式匹配指定额外的条件，仅当条件满足时才匹配成功，因此 `[var head, ..] when head == name` 的含义是，匹配至少含有一个元素的列表，并且将头元素赋值给 `head`，且仅当 `head == name` 时匹配才算成功。

最后我们再重写 `ToString` 方法方便输出表达式，就全部大功告成了。

### 测试
接下来让我测试测试我们编写的表达式计算器：

```csharp
Expr<int> a = 4;
Expr<int> b = -3;
Expr<int> x = "x";
Expr<int> c = !((a + b) * (a - b) > x);
Expr<int> y = "y";
Expr<int> z = "z";
Expr<int> expr = (c.Switch(y, z) - a > x).Switch(z + a, y / b);
Console.WriteLine(expr);
Console.WriteLine(expr.InteractiveEval());
```

运行后得到输出：

```
((((! ((((4) + (-3)) * ((4) - (-3))) > (x))) ? (y) : (z)) - (4)) > (x)) ? ((z) + (4)) : ((y) / (-3))
```

然后我们给 `x`、`y` 和 `z` 分别设置成 42、27 和 35，即可得到运算结果：

```
Parameter x: 42
Parameter y: 27
Parameter z: 35
-9
```

再测测表达式判等逻辑：

```csharp
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
```

得到输出：

```
True
False
```

## 活动模式
在未来，C# 将会引入活动模式，该模式允许用户自定义模式匹配的方法，例如：

```csharp
static bool Even<T>(this T value) where T : IBinaryInteger<T> => value % 2 == 0;
```

上述代码定义了一个 `T` 的扩展方法 `Even`，用来匹配 `value` 是否为偶数，于是我们便可以这么使用：

```csharp
var x = 3;
var y = x switch
{
    Even() => "even",
    _ => "odd"
};
```

此外，该模式还可以和解构模式结合，允许用户自定义解构行为，例如：

```csharp
static bool Int(this string value, out int result) => int.TryParse(value, out result);
```

然后使用的时候：

```csharp
var x = "3";
var y = x switch
{
    Int(var result) => result,
    _ => 0
};
```

即可对 `x` 这个字符串进行匹配，如果 `x` 可以被解析为 `int`，就取解析结果 `result`，否则取 0。

## 后记
模式匹配极大的方便了我们编写出简洁且可读性高的高质量代码，并且会自动帮我们做穷举检查，防止我们漏掉情况。此外，使用模式匹配时，编译器也会帮我们优化代码，减少完成匹配所需要的比较次数，最终减少分支并提升运行效率。

本文中的例子为了覆盖到全部的模式，不一定采用了最优的写法，这一点各位读者们也请注意。

本文中的表达式计算器全部代码可以前往我的 GitHub 仓库获取：https://github.com/hez2010/PatternMatchingExpr
