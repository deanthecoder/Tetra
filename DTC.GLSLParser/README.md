# GLSL Parser

This component is the second step in running the GLSL compiler.

It takes the flat stream of tokens emitted by the GLSL Lexer and converts them into a structured **Abstract Syntax Tree (AST)**. The AST represents the syntactic structure of the input code in a tree form, making it suitable for further analysis and transformation (e.g., code generation to Tetra bytecode).

## Overview

The parser is a **recursive descent parser** that supports a subset of C-style GLSL, including:
- Function declarations and calls
- Variable declarations and assignments
- Expressions with correct precedence and associativity
- Control flow constructs like `if`, `else`, and `while`

## Core Concepts

| **Term**        | **Meaning**                                                                 |
|-----------------|------------------------------------------------------------------------------|
| **AST**         | Abstract Syntax Tree — hierarchical structure representing code logic        |
| **Node**        | A class representing a syntactic construct, e.g., `BinaryExpr`, `IfStmt`     |
| **Expr**        | Expression node — produces a value (e.g., `a + b`, `sin(x)`)                 |
| **Stmt**        | Statement node — performs an action (e.g., assignment, return, if-block)     |
| **Parse Rule**  | A method like `ParseExpression()` or `ParseStatement()` that builds AST     |
| **Precedence**  | Determines operator binding strength (e.g., `*` binds tighter than `+`)      |
| **Associativity** | Direction an operator groups (e.g., `a - b - c` is `(a - b) - c`)          |

## AST Node Types

| **Node Type**       | **Examples**                           | **Description**                             |
|---------------------|----------------------------------------|---------------------------------------------|
| `ProgramNode`       | Entire file — a list of `FunctionNode` | Root node of the tree                        |
| `FunctionNode`      | `float main() { ... }`                 | Represents a function declaration            |
| `AssignmentNode`    | `float a = 1.0;`, `a = b + 1.0;`       | Variable assignment                          |
| `IfStatementNode`   | `if (x > 0) { ... } else { ... }`      | Conditional branch                           |
| `WhileStatementNode`| `while (i < 10) { ... }`               | Looping construct                            |
| `ReturnNode`        | `return result;`                       | Return statement                             |
| `ExprStatementNode` | `sin(x);`                              | Expression used as a statement               |
| `BinaryExprNode`    | `a + b`                                | Two-operand arithmetic or logical expression |
| `UnaryExprNode`     | `-x`, `!flag`                          | One-operand prefix expression                |
| `CallExprNode`      | `sin(x)`                               | Function call                                |
| `LiteralNode`       | `3.14`, `true`                         | Literal constant                             |
| `VariableNode`      | `a`, `uv0`                             | Variable reference                           |

## Expression Precedence (High → Low)

| **Operators**         | **Associativity**  |
|-----------------------|--------------------|
| Function call `()`    | Left               |
| Unary `+ - !`         | Right              |
| `* / %`               | Left               |
| `+ -`                 | Left               |
| Comparison `< > <= >=`| Left               |
| Equality `== !=`      | Left               |
| Logical AND `&&`      | Left               |
| Assignment `=`        | Right              |

## Parser Design Goals

- **Minimal Lookahead:** Only the current and next token are usually needed
- **Predictable Grammar:** No backtracking
- **Error Recovery:** Aims to keep parsing even after syntax errors for better diagnostics

## Example Flow

Given this input:
```glsl
float main() {
    float a = sin(b + 1.0);
    return a;
}
```

The parser would emit:
```
ProgramNode
└── FunctionNode("main")
    ├── AssignmentNode("a", CallExprNode("sin", [BinaryExprNode(Variable("b"), "+", Literal(1.0))]))
    └── ReturnNode(Variable("a"))
```

---

This structure forms a solid base for the code generator, which will traverse the AST and emit Tetra source.