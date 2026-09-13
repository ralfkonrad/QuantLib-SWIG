cmake_minimum_required(VERSION 3.20)

# SWIG target naming and module name policies
cmake_policy(SET CMP0078 NEW)
cmake_policy(SET CMP0086 NEW)
cmake_policy(SET CMP0122 NEW)

# --- Version info ---
# The version is maintained in configure.ac; parse it from there so that the
# release workflows, which rewrite AC_INIT with sed, stay the single source of
# truth and this file can never drift out of sync.
file(READ "${CMAKE_CURRENT_SOURCE_DIR}/configure.ac" _configure_ac)
if (NOT _configure_ac MATCHES "AC_INIT\\(\\[QuantLib-SWIG\\][ \t\r\n]*,[ \t\r\n]*\\[([^]]+)\\]")
    message(FATAL_ERROR "Could not determine the version from configure.ac")
endif()
set(QUANTLIB_SWIG_VERSION "${CMAKE_MATCH_1}")
unset(_configure_ac)

# R rejects version suffixes such as "-dev" or "-rc"; configure.ac strips them
# for R via PACKAGE_VERSION_FOR_R, so do the same here.
string(REGEX REPLACE "-.*$" "" QUANTLIB_SWIG_VERSION_FOR_R "${QUANTLIB_SWIG_VERSION}")

# project() needs a strictly numeric version
set(_version_numeric "${QUANTLIB_SWIG_VERSION_FOR_R}")
if (NOT _version_numeric MATCHES "^[0-9]+\\.[0-9]+\\.[0-9]+$")
    set(_version_numeric "${_version_numeric}.0")
endif()

project(QuantLib-SWIG VERSION ${_version_numeric} LANGUAGES CXX
        DESCRIPTION "SWIG language bindings for QuantLib")
unset(_version_numeric)

message(STATUS "QuantLib-SWIG version ${QUANTLIB_SWIG_VERSION} (from configure.ac)")

# Path for package-local cmake modules
set(CMAKE_MODULE_PATH "${PROJECT_SOURCE_DIR}/cmake" ${CMAKE_MODULE_PATH})

include(GNUInstallDirs)

# Classpath separator, shared by the Java and Scala bindings
if (WIN32)
    set(JAVA_CLASSPATH_SEPARATOR ";")
else()
    set(JAVA_CLASSPATH_SEPARATOR ":")
endif()

# Require C++17 or higher (matching QuantLib)
if (NOT DEFINED CMAKE_CXX_STANDARD)
    set(CMAKE_CXX_STANDARD 17)
elseif(CMAKE_CXX_STANDARD LESS 17)
    message(FATAL_ERROR "Please specify CMAKE_CXX_STANDARD of 17 or higher")
endif()
if (NOT DEFINED CMAKE_CXX_STANDARD_REQUIRED)
    set(CMAKE_CXX_STANDARD_REQUIRED ON)
endif()
if (NOT DEFINED CMAKE_CXX_EXTENSIONS)
    set(CMAKE_CXX_EXTENSIONS FALSE)
endif()

# PIC is required for shared/module libraries
if (NOT DEFINED CMAKE_POSITION_INDEPENDENT_CODE)
    set(CMAKE_POSITION_INDEPENDENT_CODE ON)
endif()

# --- Find QuantLib ---
# The location of QuantLib is an input to this build; it is never guessed.
# Supply it as either
#
#   -DCMAKE_PREFIX_PATH=<quantlib-install-prefix>
#       for an installed QuantLib (finds <prefix>/lib/cmake/QuantLib), or
#   -DQuantLib_DIR=<quantlib-build-dir>/cmake
#       to consume an uninstalled QuantLib build tree directly.
#
# CMakePresets.json is the convenient place to record either of them.
#
# The imported QuantLib::QuantLib target is deliberately the only supported
# route. It resolves its prefix relative to the config file, so it stays correct
# wherever the tree was installed, and its INTERFACE_INCLUDE_DIRECTORIES include
# Boost. The installed quantlib-config and quantlib.pc get both of these wrong:
# they bake in CMAKE_INSTALL_PREFIX as it stood at QuantLib's *configure* time
# and list no Boost include directory at all. Falling back to them would bind
# silently to the wrong QuantLib and fail later, during compilation, instead of
# here.
find_package(QuantLib REQUIRED CONFIG)
message(STATUS "Found QuantLib ${QuantLib_VERSION}: ${QuantLib_DIR}")

# --- Find SWIG ---
find_package(SWIG 4.0 REQUIRED)
include(UseSWIG)
set(UseSWIG_MODULE_VERSION 2)

# SWIG interface file (shared by all language bindings).
#
# Each language subdirectory configures this same file differently via
# set_property(SOURCE ...). That works because source file properties are
# directory-scoped, so the settings made in Python/ do not leak into Java/.
set(QUANTLIB_SWIG_INTERFACE "${PROJECT_SOURCE_DIR}/SWIG/quantlib.i")

# Extra flags handed to swig, e.g. -Werror. The autotools build exposes the
# same knob as `make SWIGFLAGS=...`, and CI passes -Werror on every build.
set(QUANTLIB_SWIG_FLAGS "" CACHE STRING
    "Additional flags passed to the swig command (e.g. -Werror)")
separate_arguments(QUANTLIB_SWIG_FLAGS_LIST NATIVE_COMMAND "${QUANTLIB_SWIG_FLAGS}")

# Warning flags for SWIG-generated C++ code (from acinclude.m4)
set(QUANTLIB_SWIG_CXX_WARNING_FLAGS)
include(CheckCXXCompilerFlag)
foreach(_flag -fno-strict-aliasing -Wno-unused -Wno-uninitialized -Wno-sign-compare -Wno-write-strings)
    string(MAKE_C_IDENTIFIER "HAS${_flag}" _var)
    check_cxx_compiler_flag(${_flag} ${_var})
    if (${_var})
        list(APPEND QUANTLIB_SWIG_CXX_WARNING_FLAGS ${_flag})
    endif()
endforeach()

# --- Detect the available language toolchains ---
# configure.ac keeps tool detection (HAVE_PYTHON, HAVE_R, ...) separate from
# intent (--enable-python, --enable-r, ...) and silently skips a binding whose
# toolchain is missing. Mirror that: probe here, and let each option default to
# whatever was found. Requesting a binding explicitly whose toolchain is absent
# still fails loudly, in the subdirectory.
find_package(Python3 QUIET COMPONENTS Interpreter Development)
set(_have_python ${Python3_FOUND})

find_package(Java QUIET COMPONENTS Development)
find_package(JNI QUIET)
if (Java_FOUND AND JNI_FOUND AND Java_JAVAC_EXECUTABLE AND Java_JAR_EXECUTABLE)
    set(_have_java TRUE)
else()
    set(_have_java FALSE)
endif()

find_program(DOTNET_EXECUTABLE dotnet)
if (DOTNET_EXECUTABLE)
    set(_have_dotnet TRUE)
else()
    set(_have_dotnet FALSE)
endif()

find_program(R_EXECUTABLE R)
if (R_EXECUTABLE)
    set(_have_r TRUE)
else()
    set(_have_r FALSE)
endif()

find_program(SCALAC_EXECUTABLE scalac)
find_program(SCALA_EXECUTABLE scala)
if (SCALAC_EXECUTABLE AND SCALA_EXECUTABLE)
    set(_have_scala TRUE)
else()
    set(_have_scala FALSE)
endif()

# --- Language binding options ---
option(QUANTLIB_SWIG_BUILD_PYTHON  "Build Python binding"  ${_have_python})
option(QUANTLIB_SWIG_BUILD_JAVA    "Build Java binding"    ${_have_java})
option(QUANTLIB_SWIG_BUILD_CSHARP  "Build C# binding"      ${_have_dotnet})
option(QUANTLIB_SWIG_BUILD_R       "Build R binding"       ${_have_r})
option(QUANTLIB_SWIG_BUILD_SCALA   "Build Scala examples"  ${_have_scala})

# Java-specific options (matching configure.ac)
option(QUANTLIB_SWIG_JAVA_AUTOLOAD      "Auto-load JNI shared library in Java" ON)
option(QUANTLIB_SWIG_JAVA_FINALIZER     "Generate Java finalizers" ON)
option(QUANTLIB_SWIG_JAVA_AUTOCLOSEABLE "Implement AutoCloseable in Java" OFF)

# Scala has no binding of its own; it uses the Java jar and JNI library.
if (QUANTLIB_SWIG_BUILD_SCALA AND NOT QUANTLIB_SWIG_BUILD_JAVA)
    message(WARNING "Scala examples require the Java binding. Enabling Java.")
    set(QUANTLIB_SWIG_BUILD_JAVA ON)
endif()

enable_testing()

# --- Add language subdirectories ---
if (QUANTLIB_SWIG_BUILD_PYTHON)
    add_subdirectory(Python)
endif()

if (QUANTLIB_SWIG_BUILD_JAVA)
    add_subdirectory(Java)
endif()

if (QUANTLIB_SWIG_BUILD_CSHARP)
    add_subdirectory(CSharp)
endif()

if (QUANTLIB_SWIG_BUILD_R)
    add_subdirectory(R)
endif()

if (QUANTLIB_SWIG_BUILD_SCALA)
    add_subdirectory(Scala)
endif()

# --- Summary ---
message(STATUS "")
message(STATUS "QuantLib-SWIG ${QUANTLIB_SWIG_VERSION} configuration summary:")
message(STATUS "  Python:  ${QUANTLIB_SWIG_BUILD_PYTHON}")
message(STATUS "  Java:    ${QUANTLIB_SWIG_BUILD_JAVA}")
message(STATUS "  C#:      ${QUANTLIB_SWIG_BUILD_CSHARP}")
message(STATUS "  R:       ${QUANTLIB_SWIG_BUILD_R}")
message(STATUS "  Scala:   ${QUANTLIB_SWIG_BUILD_SCALA}")
if (QUANTLIB_SWIG_BUILD_JAVA)
    message(STATUS "  Java autoload:      ${QUANTLIB_SWIG_JAVA_AUTOLOAD}")
    message(STATUS "  Java finalizer:     ${QUANTLIB_SWIG_JAVA_FINALIZER}")
    message(STATUS "  Java autocloseable: ${QUANTLIB_SWIG_JAVA_AUTOCLOSEABLE}")
endif()
if (QUANTLIB_SWIG_FLAGS)
    message(STATUS "  Extra SWIG flags:   ${QUANTLIB_SWIG_FLAGS}")
endif()
message(STATUS "")
