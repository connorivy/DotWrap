# Contributing to DotWrap

Thank you for your interest in contributing to DotWrap! This project bridges .NET and Python ecosystems, so contributions from both C# and Python developers are welcome.

## Getting Started

1. **Fork the repository** and clone it locally.
2. **Set up your environment:**
    - .NET 8+ SDK (for building and running C# projects)
    - Python 3.8+ (for testing Python bindings)
    - (Optional) Visual Studio or VS Code for development
    - (Optional) Docker for devcontainer support
3. **Install Python dependencies:**
    ```sh
    pip install -r requirements.txt
    ```
4. **Build the .NET projects:**
    ```sh
    dotnet build
    ```

## Testing

-   **.NET Unit Tests:**
    ```sh
    dotnet test
    ```
-   **Python Tests:**
    ```sh
    ./scripts/python_tests.sh
    ```
-   **Benchmarks:**
    -   See `tests/DotWrap.PythonTests/bench_*.py` for performance tests.

## Code Style

-   **C#:**
    -   Use standard .NET formatting and naming conventions.
    -   Add XML documentation comments for public APIs.
-   **Python:**
    -   Follow PEP8 style.
    -   Add docstrings to all public functions/classes.

## Pull Requests

-   Create a new branch for your change.
-   Write clear commit messages.
-   Add or update tests as needed.
-   Ensure all tests pass before submitting a PR.
-   Fill out the PR template and describe your changes.

## Reporting Issues

-   Use the issue tracker to report bugs or request features.
-   Include steps to reproduce, expected/actual behavior, and environment details.

## Additional Resources

-   See `.github/copilot-instructions.md` for architecture and conventions.
-   See `README.md` for project overview and usage.
-   See `wiki/` for detailed documentation.

We appreciate your contributions!
