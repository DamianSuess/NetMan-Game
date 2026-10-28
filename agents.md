# Coding conventions

## C# class member order

- Place all `extern` method declarations at the top of the containing class, followed by all nested `struct` and `record struct` declarations.
- Keep both groups before fields, constructors, properties, regular methods, and other nested types, regardless of access level.
- Keep attributes, documentation, and comments attached to their declarations when moving them.
- Apply this ordering when adding or editing a class. Preserve the existing ordering within each group.
- When writing, editing, or refactoring C# code, strictly adhere to whitespace rules.
- **Rule:** Every closing curly brace `}` that terminates a statement block (methods, properties, loops, if/else statements, classes) must be followed by exactly one empty blank line.
- Do not cluster code blocks tightly together.
