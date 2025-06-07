# GLSL Lexer

This component is the first step in running the GLSL compiler.

It is a standalone C# lexer designed to tokenize a subset of GLSL/C-style shader code. Initially, it focuses on core language features like types, identifiers, literals, and operators. The lexer produces a flat token stream which will later be parsed into structured statements and expressions.

## Token Categories

The lexer emits tokens according to the following categories:

| **Category**     | **Examples**                    | **Token Type**                              | **Description**                     |
|------------------|----------------------------------|---------------------------------------------|-------------------------------------|
| **Literals**     | `123`, `3.14`, `true`            | `IntLiteral`, `FloatLiteral`, `BoolLiteral` | Concrete values used in code        |
| **Identifiers**  | `x`, `main`, `uv0`              | `Identifier`                                | User-defined variable/function names |
| **Keywords**     | `float`, `return`, `uniform`     | `Keyword`                                   | Reserved words in the language      |
| **Operators**    | `+`, `-`, `*`, `/`, `==`, `!=`   | `Plus`, `Minus`, `Asterisk`, etc.           | Perform operations on values        |
| **Punctuation**  | `;`, `,`, `(`, `)`, `{`, `}`     | `Semicolon`, `Comma`, `LeftParen`, etc.     | Structural syntax markers           |
| **Misc**         | whitespace, comments             | `Whitespace`, `Comment`                     | Non-code              |
