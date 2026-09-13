
QuantLib-SWIG: language bindings for QuantLib
=============================================

[![Download source](https://img.shields.io/github/v/release/lballabio/QuantLib-SWIG?label=source&sort=semver)](https://github.com/lballabio/QuantLib-SWIG/releases/latest)
[![PyPI version](https://img.shields.io/pypi/v/quantlib?label=PyPI)](https://pypi.org/project/QuantLib)
![PRs Welcome](https://img.shields.io/badge/PRs%20-welcome-brightgreen.svg)
[![DOI](https://zenodo.org/badge/DOI/10.5281/zenodo.1441003.svg)](https://doi.org/10.5281/zenodo.1441003)
[![Build status](https://github.com/lballabio/QuantLib-SWIG/workflows/Linux%20build/badge.svg?branch=master)](https://github.com/lballabio/QuantLib-SWIG/actions?query=workflow%3A%22Linux+build%22)
[![Binder](https://mybinder.org/badge_logo.svg)](https://mybinder.org/v2/gh/lballabio/QuantLib-SWIG/binder?urlpath=lab/tree/Python/examples)

---

The QuantLib project (<https://www.quantlib.org>) is aimed at providing a
comprehensive software framework for quantitative finance. QuantLib is
a free/open-source library for modeling, trading, and risk management
in real-life.

QuantLib is Non-Copylefted Free Software and OSI Certified Open Source
Software.

QuantLib-SWIG provides the means to use QuantLib from a number of
languages; currently their list includes Python, C#, Java, Scala and R.


Questions and feedback
----------------------

Bugs can be reported as a GitHub issue at
<https://github.com/lballabio/QuantLib-SWIG/issues>; if you have a
patch available, you can open a pull request instead (see
"Contributing" below).

You can also use the `quantlib-users` and `quantlib-dev` mailing lists
for feedback, questions, etc.  More information and instructions for
subscribing are at <https://www.quantlib.org/mailinglists.shtml>.



Building
--------

The canonical build is the autotools one:

```
./autogen.sh
./configure
make
```

A CMake build is available alongside it. It builds and tests the same bindings,
but it does not replace autotools: source distributions (`make dist`), the
Python wheels published to PyPI and the NuGet package are all still produced by
the autotools build.

The CMake build requires a QuantLib that was itself built with CMake, and its
location is an explicit input — it is never guessed:

```
# against an installed QuantLib
cmake -S . -B build -DCMAKE_PREFIX_PATH=/path/to/quantlib/prefix

# or directly against an uninstalled QuantLib build tree
cmake -S . -B build -DQuantLib_DIR=/path/to/quantlib/build/cmake

cmake --build build
ctest --test-dir build --output-on-failure
```

Bindings whose toolchain is not installed are skipped; the configuration summary
lists what was enabled. Each can be forced on or off explicitly:

| Option | Default |
| --- | --- |
| `QUANTLIB_SWIG_BUILD_PYTHON` | on if Python 3 with development headers is found |
| `QUANTLIB_SWIG_BUILD_JAVA` | on if a JDK and JNI headers are found |
| `QUANTLIB_SWIG_BUILD_CSHARP` | on if `dotnet` is found |
| `QUANTLIB_SWIG_BUILD_R` | on if `R` is found |
| `QUANTLIB_SWIG_BUILD_SCALA` | on if `scalac` and `scala` are found |
| `QUANTLIB_SWIG_FLAGS` | extra flags for `swig`, e.g. `-Werror` |

`CMakePresets.json` provides `default`, `debug`, `strict` (SWIG warnings as
errors) and `python-only` presets; set the `QUANTLIB_PREFIX` environment
variable, or record your own paths in a `CMakeUserPresets.json`.


Contributing
------------

The easiest way to contribute is through pull requests on GitHub.  Get
a GitHub account if you don't have it already and clone the repository
at <https://github.com/lballabio/QuantLib-SWIG> with the "Fork" button
in the top right corner of the page. Check out your clone to your
machine, code away, push your changes to your clone and submit a pull
request; instructions are available at
<https://help.github.com/articles/fork-a-repo>.  (In case you need
them, more detailed instructions for creating pull requests are at
<https://help.github.com/articles/using-pull-requests>, and a basic
guide to GitHub is at
<https://guides.github.com/activities/hello-world/>.

It's likely that we won't merge your code right away, and we'll ask
for some changes instead. Don't be discouraged! That's normal; the
library is complex, and thus it might take some time to become
familiar with it and to use it in an idiomatic way.

We're looking forward to your contributions.
