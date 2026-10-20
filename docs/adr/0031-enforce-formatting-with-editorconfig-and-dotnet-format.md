# Enforce formatting with EditorConfig and CSharpier

Backend repositories define formatting rules in `.editorconfig` and verify them
automatically with CSharpier.

## Considered Options

Formatting rules could be documented and left to individual developers and IDEs.

With roughly thirty developers, different IDE defaults and incomplete manual
adherence would produce unnecessary formatting differences and review noise.

A machine-enforced formatter makes the repository configuration the single source
of truth instead of relying on every contributor to reproduce the same local
settings. CSharpier additionally formats fluent API chains consistently, which
`dotnet format` cannot configure through `.editorconfig`.

## Consequences

Formatting differences are detected automatically before or during CI. The
versioned local .NET tool guarantees identical output on developer machines and
in CI.

Developers may use different IDEs while producing the same repository style.

Formatting rules should remain minimal and should not be expanded into subjective
style policies without a shared decision.
