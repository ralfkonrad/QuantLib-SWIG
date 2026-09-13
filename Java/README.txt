
On Linux systems, the module can be build by supplying the location of
the JDK to configure, as in (for example)

./autogen.sh
./configure --with-jdk-include=/usr/lib/jvm/java-1.5.0-sun-1.5.0.08/include \
  --with-jdk-system-include=usr/lib/jvm/java-1.5.0-sun-1.5.0.08/include/linux

and by running 'make' afterwards.


Building with CMake
-------------------

A CMake build is available as an alternative to the autotools build described
above; see the "Building" section of the top-level README.md. It requires a
CMake-built QuantLib, whose location must be passed in explicitly via
CMAKE_PREFIX_PATH or QuantLib_DIR.
