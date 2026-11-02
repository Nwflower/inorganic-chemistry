"""Compatibility entry point for the current shared gas-bottle generator."""

from generate_native_fit_assets import generate


if __name__ == "__main__":
    generate()
    print("Wrote four narrow gas bottles, plasma-style fuel states and rack states.")
