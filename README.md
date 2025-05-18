# Tetra VM

Tetra is a lightweight, stack-based virtual machine written in C#, designed to execute a custom low-level language
called **Tetra**. It's inspired by GLSL and designed with vector math, scoped variables, and clean, debuggable control
flow in mind. The name comes from the Greek word for "four," highlighting its support for 4-element float vectors (
`vec4`).

---

## Features

- **Scoped Variable Stack**: Each block or function has its own variable frame. Stack frames are explicitly
  pushed/popped using `push_frame` / `pop_frame`.
- **Named Variables**: No general-purpose registers. All values are stored and looked up by name.
- **Conditional and Unconditional Jumps**: Support for jump labels like `mylabel:` and instructions like `jmp`,
  `jmp_ne`, `jmp_ge`, etc.
- **4-element Float Vector Support**: Planned support for `vec4` operations alongside scalar floats.
- **Basic Arithmetic**: Instructions like `add`, `sub`, `inc`, `dec`.
- **Debugging Aids**: A `print` instruction that outputs variable values along with the line number of the source
  instruction.
- **Globals Support**: A global frame is automatically pushed on startup. Any code before `jmp main` is treated as
  global initialization.
- **Manual Control Flow**: The VM does not auto-jump to `main:`; you must explicitly add `jmp main` at the end of your
  global setup.

---

## Instruction Set (Initial Subset)

| Instruction     | Description |
|-----------------|-------------|
| `ld $a, 1.0`     | Load constant into `$a` |
| `ld $b, $a`      | Copy variable `$a` into `$b` |
| `add $a, $b`     | `$a = $a + $b` |
| `sub $a, $b`     | `$a = $a - $b` |
| `inc $a`         | `$a = $a + 1` |
| `dec $a`         | `$a = $a - 1` |
| `jmp label`      | Unconditional jump |
| `jmp_ne $a, $b, label` | Jump if `$a != $b` |
| `jmp_ge $a, $b, label` | Jump if `$a >= $b` |
| `call func`      | Call subroutine at `func:` |
| `ret`            | Return from function |
| `push_frame`     | Start new variable scope |
| `pop_frame`      | Exit current variable scope |
| `print $a`       | Print the value of `$a` with line number |
| `halt`           | Stop execution |
| `convi $a`        | Convert `$a` from float to int (truncates) |
| `convf $a`        | Convert `$a` from int to float             |

---

## Example Tetra Program

```tetra
ld $globalX, 1.0
ld $globalY, 2.0
jmp main

main:
    push_frame
    ld $i, 0

loop:
    jmp_ge $i, 5, end
    print $i
    inc $i
    jmp loop

end:
    pop_frame
    halt
```

---

## Future Plans

- Add support for `vec4` types and operations
- GLSL-to-Tetra compiler frontend
- Function parameters and `out` values
- Optimizer pass (e.g. removing unnecessary frame pushes)
- Debug trace mode with full source context

---

## Status

🚧 Work in progress. Instruction set and behavior are still evolving.

---