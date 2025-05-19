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


## Instruction Set

### 🔢 Arithmetic

| Instruction     | Description |
|-----------------|-------------|
| `add $a, $b`    | `$a = $a + $b` |
| `sub $a, $b`    | `$a = $a - $b` |
| `mul $a, $b`    | `$a = $a * $b` |
| `div $a, $b`    | `$a = $a / $b` |
| `inc $a`        | `$a = $a + 1` |
| `dec $a`        | `$a = $a - 1` |
| `neg $a`        | `$a = -$a` (negate the value in `$a`) |

### 🔄 Control Flow

| Instruction              | Description |
|--------------------------|-------------|
| `jmp label`              | Unconditional jump |
| `jmp_eq $a, $b, label`   | Jump if `$a == $b` |
| `jmp_ne $a, $b, label`   | Jump if `$a != $b` |
| `jmp_lt $a, $b, label`   | Jump if `$a < $b` |
| `jmp_le $a, $b, label`   | Jump if `$a <= $b` |
| `jmp_gt $a, $b, label`   | Jump if `$a > $b` |
| `jmp_ge $a, $b, label`   | Jump if `$a >= $b` |

### 📦 Variables and Frames

| Instruction         | Description |
|----------------------|-------------|
| `ld $a, 1.0`         | Load constant into `$a` |
| `ld $b, $a`          | Copy variable `$a` into `$b` |
| `push_frame`         | Push a new scope frame manually (used for block scoping) |
| `pop_frame`          | Pop the current scope frame |

### 🔁 Function Calls

| Instruction      | Description |
|------------------|-------------|
| `call label`     | Call function at `label` (creates a new scope frame and pushes return address) |
| `ret`            | Return from function (restores return address and previous scope) |
| `ret $a`         | Return a value; sets `$retval` in the caller's scope |

### 🐞 Debugging & Program Control

| Instruction  | Description |
|--------------|-------------|
| `print $a`   | Print the value of `$a` with line number |
| `halt`       | Stop execution |

---

## Example Tetra Program

```
ld $globalX, 1.0
ld $globalY, 2.0
jmp main

main:
    ld $i, 0

loop:
    jmp_ge $i, 5, end
    print $i
    inc $i
    jmp loop

end:
    halt
```

---

## 🔁 Function Calls

Tetra supports calling functions using the `call` instruction, with optional return values via `ret $value`.

### Argument Passing Convention

To pass arguments to a function, use `ld` to define named variables like `$arg0`, `$arg1`, etc., **before** calling the function:

```
ld $arg0, 10
ld $arg1, 20
call my_function
```

When `call` is executed:
- A new scope frame is pushed onto the call stack.
- The function code begins execution at the specified label.
- All variables, including `$arg0`, `$arg1`, etc., remain **shared with the caller** because they were defined outside the frame. As such, these arguments behave like **`out` parameters**.

If the function modifies `$arg0`, the change is visible to the caller:
```
add $arg0, 5  # caller sees modified value
```

### Creating Isolated Parameters (Read-Only Style)

If you want function parameters to behave like 'pass by value' (copied and isolated), you must explicitly copy them into local variables at the start of the function:

```
ld $a, $arg0
ld $b, $arg1
```

This way, modifications to `$a` and `$b` do not affect the caller's `$arg0`/`$arg1`.

### Returning Values

To return a value, use `ret $value`. This sets `$retval` in the caller’s frame:

```
ret $result
```

In the caller:

```
call my_function
ld $x, $retval
```

If no value is returned, simply use `ret`.

### Example

```
    ld $arg0, 5
    call double
    ld $x, $retval
    print $x
    halt

double:
    ld $a, $arg0
    add $a, $a
    ret $a
```
```
Output: x = 10
```

---

## Future Plans

- Add support for `vec4` types and operations
- GLSL-to-Tetra compiler frontend
- Function parameters and `out` values
- Optimizer pass (e.g. removing unnecessary frame pushes)

---

## Status

🚧 Work in progress. Instruction set and behavior are still evolving.

---