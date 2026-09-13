# C# binding for QuantLib-SWIG
#
# Produces:
#   - libNQuantLibc.so (.dylib on macOS, NQuantLibc.dll on Windows) — native library
#   - NQuantLib.dll — managed assembly, via dotnet build
#
# Producing the NuGet package remains the job of the autotools build
# (`make -C CSharp nupkg`) and of .github/workflows/nupkg.yml, which aggregates
# native libraries built on four platforms. That is not reproduced here.

find_program(DOTNET_EXECUTABLE dotnet REQUIRED)

set(QUANTLIB_SWIG_DOTNET_FRAMEWORK "net9.0" CACHE STRING
    "Target framework moniker used for the C# assembly and examples")

# Output directories for SWIG-generated files. The layout deliberately mirrors
# the source tree (csharp/ next to cpp/) so that the generated project file can
# use the same relative path to the native library as the checked-in one.
set(CSHARP_SWIG_OUTDIR "${CMAKE_CURRENT_BINARY_DIR}/csharp")
set(CSHARP_SWIG_CXXDIR "${CMAKE_CURRENT_BINARY_DIR}/cpp")

file(MAKE_DIRECTORY "${CSHARP_SWIG_OUTDIR}")
file(MAKE_DIRECTORY "${CSHARP_SWIG_CXXDIR}")

# Configure the SWIG source file. Source properties are directory-scoped, so
# these settings apply to this binding only.
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY CPLUSPLUS ON)
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY USE_TARGET_INCLUDE_DIRECTORIES ON)
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY USE_SWIG_DEPENDENCIES TRUE)
# As for Java, UseSWIG_MODULE_VERSION 2 removes the previous run's generated
# .cs files before regenerating them, which is what `rm -f csharp/*.cs` does
# in CSharp/Makefile.am. The SDK compiles every .cs it finds, so this matters.
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY COMPILE_OPTIONS
    -namespace QuantLib ${QUANTLIB_SWIG_FLAGS_LIST})
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY
    SWIG_MODULE_NAME NQuantLibc)

# Use SWIG to generate the C# wrapper
swig_add_library(NQuantLibc
    TYPE SHARED
    LANGUAGE csharp
    OUTPUT_DIR "${CSHARP_SWIG_OUTDIR}"
    OUTFILE_DIR "${CSHARP_SWIG_CXXDIR}"
    SOURCES "${QUANTLIB_SWIG_INTERFACE}")

target_link_libraries(NQuantLibc PRIVATE QuantLib::QuantLib)
target_compile_options(NQuantLibc PRIVATE ${QUANTLIB_SWIG_CXX_WARNING_FLAGS})

# Match the autotools output name (libNQuantLibc.so / .dylib)
set_target_properties(NQuantLibc PROPERTIES
    OUTPUT_NAME "NQuantLibc"
    LIBRARY_OUTPUT_DIRECTORY "${CSHARP_SWIG_CXXDIR}"
    RUNTIME_OUTPUT_DIRECTORY "${CSHARP_SWIG_CXXDIR}"
)
if (NOT WIN32)
    set_target_properties(NQuantLibc PROPERTIES PREFIX "lib")
endif()

# --- Build NQuantLib.dll via dotnet ---
# Runtime identifier and native library name, used to place the native library
# where the .NET loader will find it.
if (WIN32)
    set(QUANTLIB_SWIG_DOTNET_RID "win-x64")
elseif (APPLE)
    set(QUANTLIB_SWIG_DOTNET_RID "osx-x64")
else()
    set(QUANTLIB_SWIG_DOTNET_RID "linux-x64")
endif()

set(QUANTLIB_SWIG_DOTNET_VERSION "${PROJECT_VERSION}")
set(QUANTLIB_SWIG_NATIVE_LIBRARY "$<TARGET_FILE:NQuantLibc>")
set(QUANTLIB_SWIG_NATIVE_LIBRARY_NAME "$<TARGET_FILE_NAME:NQuantLibc>")

set(NQUANTLIB_CSPROJ "${CSHARP_SWIG_OUTDIR}/NQuantLib.csproj")
configure_file("${PROJECT_SOURCE_DIR}/cmake/NQuantLib.csproj.in"
    "${NQUANTLIB_CSPROJ}.in" @ONLY)
file(GENERATE OUTPUT "${NQUANTLIB_CSPROJ}" INPUT "${NQUANTLIB_CSPROJ}.in")

set(NQUANTLIB_DLL "${CSHARP_SWIG_OUTDIR}/bin/Release/${QUANTLIB_SWIG_DOTNET_FRAMEWORK}/NQuantLib.dll")

add_custom_command(
    OUTPUT "${NQUANTLIB_DLL}"
    COMMAND "${DOTNET_EXECUTABLE}" build --nologo -c Release
        "${NQUANTLIB_CSPROJ}"
    DEPENDS NQuantLibc "${NQUANTLIB_CSPROJ}"
    WORKING_DIRECTORY "${CSHARP_SWIG_OUTDIR}"
    COMMENT "Building NQuantLib.dll via dotnet"
    VERBATIM
)

add_custom_target(NQuantLib-dll ALL DEPENDS "${NQUANTLIB_DLL}")

# --- Examples, used as the test suite (mirrors check-local) ---
set(CSHARP_EXAMPLES
    BermudanSwaption
    EquityOption
    FiniteDifferenceMethods
    Times
)

set(CSHARP_EXAMPLE_STAMPS)
foreach(_example ${CSHARP_EXAMPLES})
    set(EXAMPLE_NAME "${_example}")
    set(EXAMPLE_SOURCE "${CMAKE_CURRENT_SOURCE_DIR}/examples/${_example}/${_example}.cs")
    set(_proj_dir "${CMAKE_CURRENT_BINARY_DIR}/examples/${_example}")
    set(_proj "${_proj_dir}/${_example}.csproj")

    configure_file("${PROJECT_SOURCE_DIR}/cmake/Example.csproj.in" "${_proj}.in" @ONLY)
    file(GENERATE OUTPUT "${_proj}" INPUT "${_proj}.in")

    set(_stamp "${_proj_dir}/${_example}.stamp")
    add_custom_command(
        OUTPUT "${_stamp}"
        COMMAND "${DOTNET_EXECUTABLE}" build --nologo -c Release "${_proj}"
        COMMAND ${CMAKE_COMMAND} -E touch "${_stamp}"
        DEPENDS "${EXAMPLE_SOURCE}" "${_proj}" "${NQUANTLIB_DLL}"
        WORKING_DIRECTORY "${_proj_dir}"
        COMMENT "Building C# example: ${_example}"
        VERBATIM
    )
    list(APPEND CSHARP_EXAMPLE_STAMPS "${_stamp}")

    add_test(NAME csharp-example-${_example}
        COMMAND "${DOTNET_EXECUTABLE}"
                "${_proj_dir}/bin/Release/${QUANTLIB_SWIG_DOTNET_FRAMEWORK}/${_example}.dll")
    set_tests_properties(csharp-example-${_example} PROPERTIES
        ENVIRONMENT "LD_LIBRARY_PATH=${CSHARP_SWIG_CXXDIR};DYLD_LIBRARY_PATH=${CSHARP_SWIG_CXXDIR}")
endforeach()

add_custom_target(QuantLib-CSharp-examples ALL DEPENDS ${CSHARP_EXAMPLE_STAMPS})

# --- Installation ---
install(TARGETS NQuantLibc
    LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR}
    RUNTIME DESTINATION ${CMAKE_INSTALL_BINDIR})

install(FILES "${NQUANTLIB_DLL}"
    DESTINATION ${CMAKE_INSTALL_LIBDIR})
