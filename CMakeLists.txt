cmake_minimum_required(VERSION 3.18)

# SWIG target naming and module name policies
cmake_policy(SET CMP0078 NEW)
cmake_policy(SET CMP0086 NEW)
cmake_policy(SET CMP0122 NEW)

# Version info
set(QUANTLIB_SWIG_VERSION "1.42-dev")

project(QuantLib-SWIG VERSION 1.42.0 LANGUAGES CXX
        DESCRIPTION "SWIG language bindings for QuantLib")

# Path for package-local cmake modules
set(CMAKE_MODULE_PATH "${PROJECT_SOURCE_DIR}/cmake" ${CMAKE_MODULE_PATH})

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
# Try to find QuantLib via CMake config first, then fall back to pkg-config
find_package(QuantLib QUIET CONFIG)
if (NOT QuantLib_FOUND)
    find_package(PkgConfig)
    if (PkgConfig_FOUND)
        pkg_check_modules(QuantLib REQUIRED IMPORTED_TARGET quantlib)
        # Create an alias target matching the CMake config export name
        if (NOT TARGET QuantLib::QuantLib)
            add_library(QuantLib::QuantLib ALIAS PkgConfig::QuantLib)
        endif()
    else()
        message(FATAL_ERROR
            "QuantLib not found. Please install QuantLib and ensure it is "
            "discoverable via CMake config (QuantLibConfig.cmake) or pkg-config.")
    endif()
endif()
message(STATUS "Found QuantLib")

# --- Find SWIG ---
find_package(SWIG 4.0 REQUIRED)
include(UseSWIG)
set(UseSWIG_MODULE_VERSION 2)

# SWIG interface file (shared by all language bindings)
set(QUANTLIB_SWIG_INTERFACE "${PROJECT_SOURCE_DIR}/SWIG/quantlib.i")

# Collect all .i files for dependency tracking
file(GLOB SWIG_INTERFACE_FILES "${PROJECT_SOURCE_DIR}/SWIG/*.i")

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

# --- Language binding options ---
option(QUANTLIB_SWIG_BUILD_PYTHON  "Build Python binding"  ON)
option(QUANTLIB_SWIG_BUILD_JAVA    "Build Java binding"    ON)
option(QUANTLIB_SWIG_BUILD_CSHARP  "Build C# binding"      ON)
option(QUANTLIB_SWIG_BUILD_R       "Build R binding"        ON)
option(QUANTLIB_SWIG_BUILD_SCALA   "Build Scala examples"   ON)

# Java-specific options (matching configure.ac)
option(QUANTLIB_SWIG_JAVA_AUTOLOAD      "Auto-load JNI shared library in Java" ON)
option(QUANTLIB_SWIG_JAVA_FINALIZER     "Generate Java finalizers" ON)
option(QUANTLIB_SWIG_JAVA_AUTOCLOSEABLE "Implement AutoCloseable in Java" OFF)

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
    if (NOT QUANTLIB_SWIG_BUILD_JAVA)
        message(WARNING "Scala examples require Java binding. Enabling Java.")
        set(QUANTLIB_SWIG_BUILD_JAVA ON)
        add_subdirectory(Java)
    endif()
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
message(STATUS "")
