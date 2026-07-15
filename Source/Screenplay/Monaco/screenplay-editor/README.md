# Screenplay Editor

Standalone Vite + React host for the Monaco-based Screenplay editor. Loads the complete `invoicing.play` sample on startup with full syntax highlighting, IntelliSense, and diagnostics from [`@cratis/screenplay-language`](../screenplay-language).

## Running

From the repository root (builds the language package first):

```shell
yarn build
yarn dev
```

The editor starts on <http://localhost:9200>.

## Toolbar

- **New** — clears the buffer (with confirmation).
- **Open…** — opens a `.play` file from disk.
- **Save** — downloads the current buffer as a `.play` file.
- **Light/Dark theme** — toggles between the `screenplay-light` and `screenplay-dark` themes.
