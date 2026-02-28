# C# binding for QuantLib-SWIG
#
# Produces:
#   - libNQuantLibc.so (.dylib on macOS) — native shared library
#   - Optionally: NQuantLib.dll via dotnet build (if dotnet is found)

# Output directories for SWIG-generated files
set(CSHARP_SWIG_OUTDIR "${CMAKE_CURRENT_BINARY_DIR}/csharp")
set(CSHARP_SWIG_CXXDIR "${CMAKE_CURRENT_BINARY_DIR}/cpp")

file(MAKE_DIRECTORY "${CSHARP_SWIG_OUTDIR}")
file(MAKE_DIRECTORY "${CSHARP_SWIG_CXXDIR}")

# Configure the SWIG source file
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY CPLUSPLUS ON)
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY USE_TARGET_INCLUDE_DIRECTORIES ON)
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY COMPILE_OPTIONS
    "-namespace" "QuantLib")
set_property(SOURCE "${QUANTLIB_SWIG_INTERFACE}" PROPERTY
    SWIG_MODULE_NAME NQuantLibc)

# Use SWIG to generate C# wrapper
swig_add_library(NQuantLibc
    TYPE SHARED
    LANGUAGE csharp
    OUTPUT_DIR "${CSHARP_SWIG_OUTDIR}"
    OUTFILE_DIR "${CSHARP_SWIG_CXXDIR}"
    SOURCES "${QUANTLIB_SWIG_INTERFACE}")

# Link against QuantLib
target_link_libraries(NQuantLibc PRIVATE QuantLib::QuantLib)

# Apply warning flags for SWIG-generated code
target_compile_options(NQuantLibc PRIVATE ${QUANTLIB_SWIG_CXX_WARNING_FLAGS})

# SWIG .i file dependencies
set_property(TARGET NQuantLibc PROPERTY SWIG_DEPENDS ${SWIG_INTERFACE_FILES})

# Set output properties to match autotools (libNQuantLibc.so / .dylib)
set_target_properties(NQuantLibc PROPERTIES
    OUTPUT_NAME "NQuantLibc"
    PREFIX "lib"
    LIBRARY_OUTPUT_DIRECTORY "${CSHARP_SWIG_CXXDIR}"
)

# --- Optionally build NQuantLib.dll via dotnet ---
find_program(DOTNET_EXECUTABLE dotnet)

if (DOTNET_EXECUTABLE)
    set(NQUANTLIB_DLL "${CSHARP_SWIG_OUTDIR}/bin/Release/net9.0/NQuantLib.dll")

    add_custom_command(
        OUTPUT "${NQUANTLIB_DLL}"
        COMMAND "${DOTNET_EXECUTABLE}" build --nologo
            -c Release --framework net9.0
            -p:Version=${QUANTLIB_SWIG_VERSION}
            "${CMAKE_CURRENT_SOURCE_DIR}/csharp/NQuantLib.csproj"
        DEPENDS NQuantLibc
        WORKING_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}"
        COMMENT "Building NQuantLib.dll via dotnet"
        VERBATIM
    )

    add_custom_target(NQuantLib-dll ALL DEPENDS "${NQUANTLIB_DLL}")
else()
    message(STATUS "dotnet not found — skipping NQuantLib.dll build (native library will still be built)")
endif()

# --- Installation ---
install(TARGETS NQuantLibc
    LIBRARY DESTINATION lib
    RUNTIME DESTINATION bin)
