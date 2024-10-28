#!/bin/bash

xml_file=$1

BOLD='\033[1;1m'
BBLUE='\033[1;34m'
BRED='\033[1;31m'
BGREEN='\033[1;32m'
RED='\033[0;31m'
GREEN='\033[0;32m'
GREY='\033[0;90m'
NC='\033[0m'

# Task 1: Get the list of "classname" attribute values for all "test-suite" tags
classnames=$(xmlstarlet sel -t -m "//test-suite" -v "@classname" -n "$xml_file")

# Keep track of the total number of tests passed and failed, summing the list
total_passed=0
total_failed=0

for classname in $classnames; do
    current_suite="//test-suite[@classname='$classname']"

    fullname=$(xmlstarlet sel -t -m "$current_suite" -v "@fullname" -n "$xml_file")
    passed=$(xmlstarlet sel -t -m "$current_suite" -v "@passed" -n "$xml_file")
    failed=$(xmlstarlet sel -t -m "$current_suite" -v "@failed" -n "$xml_file")
    
    total_passed=$((total_passed + passed))
    total_failed=$((total_failed + failed))
    
    summary="$BGREEN${passed}~P/${failed}~F$NC"
    if [ "$failed" -ne 0 ]; then
        summary="$BGREEN${passed}~passed/$BRED${failed}~failed$NC"
    fi

    # Go through each test case of each test suite. For each:
    #   - Print any logged output, and
    #   - If the test failed, print the failure message and stack trace
    printf "$BBLUE%-66s%s\n" "$fullname~" "~[$passed~passed~/~${failed}~failed]" | tr ' ~' '- '
    testcases=$(xmlstarlet sel -t -m "//test-suite[@classname='$classname']/test-case" -v "@name" -n "$xml_file")
    for testcase in $testcases; do
        current_testcase="$current_suite/test-case[@name='$testcase']"

        status=$(xmlstarlet sel -t -m "$current_testcase" -v "@result" -n "$xml_file")
        output=$(xmlstarlet sel -t -m "$current_testcase/output" -v "." -n "$xml_file")
        failure_message=$(xmlstarlet sel -t -m "$current_testcase/failure/message" -v "." -n "$xml_file")
        stack_trace=$(xmlstarlet sel -t -m "$current_testcase/failure/stack-trace" -v "." -n "$xml_file")
        if [ "$status" = "Failed" ]; then
            printf "$RED%-66s%s\n" "$testcase~" "~$status" | tr ' ~' '- '
            printf "$GREY%s\n$NC" "$failure_message" | sed 's/^/    /'
            printf "$GREY%s\n$NC" "$stack_trace" | sed 's/^/    /'
        else
            printf "$GREEN%-66s%s\n$NC" "$testcase~" "~$status" | tr ' ~' '- '
        fi
        printf "$GREY%s$NC\n" "$output" | sed 's/^/    /'
    done
done

# Task 3: Print the total number of tests passed and failed overall (summed)
printf "${BBLUE}Total: $total_passed tests passed, $total_failed tests failed$NC\n"
