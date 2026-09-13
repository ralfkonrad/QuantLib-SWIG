
Visual Studio .NET projects are provided; note that before launching
the IDE, you'll have to define an environment variable QL_DIR whose
value must equal the path to your QuantLib installation, e.g.,
"C:\Lib\QuantLib".

The interfaces also work with .Net on Linux and macOS; run 'make' in
this directory after running './autogen.sh' and './configure' in the
main QuantLib-SWIG directory.


Building with CMake
-------------------

A CMake build is available as an alternative to the autotools build described
above; see the "Building" section of the top-level README.md. It requires a
CMake-built QuantLib, whose location must be passed in explicitly via
CMAKE_PREFIX_PATH or QuantLib_DIR.
