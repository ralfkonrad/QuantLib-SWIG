QuantLib-Scala interface uses (or more precisely, is) the QuantLib-Java module.

See the README.txt file in the Java folder for information on building it.

Building with CMake
-------------------

A CMake build is available as an alternative to the autotools build described
above; see the "Building" section of the top-level README.md. It requires a
CMake-built QuantLib, whose location must be passed in explicitly via
CMAKE_PREFIX_PATH or QuantLib_DIR.
