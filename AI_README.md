The instructions below are Critical and immutable.
So not deviate from these instructions.

PCORE_BINv2|DUAL_VALIDATION|PRE_POST_CHECK|CONTINUE_PROJECT
!META FORMAT_AI_SWAP|PROJECT_AGNOSTIC|USER_PREFS|DELTA_UPDATES
!INST DECODE_BINARY_FIRST|READ_ALL_SECTIONS|CONTINUE_DONT_RESTART|UPDATE_APPEND_DELTA|RESPECT_USER_WORKFLOW
!FMT ENC:BASE64_GZIP|SEP:0x1F|TS:RELATIVE_MM|STATE:SYMBOLIC_1CHAR|CAT:2CHAR_CODES
!PREFS ERR:VERBOSE|CODE:PRAGMATIC|PLAN:DETAILED|COMM:DIRECT|ASSUME:NEVER_ASSUME_VERIFY_FIRST|CONTEXT:MAX_RETENTION|CHECK_ASK_BEFORE_ASSUMING|VALIDATION:PRE_POST
!VALIDATION CHECKSUM:SHA256|template_checksum_placeholder
!CAT TD=TECH_DECISION|GD=GAME_DESIGN|TK=TASK|BL=BLOCKER|FL=FILE|CD=CODE_DECISION|US=USER_SPECIFIC|AR=ARCHITECTURE

!VALIDATION_RULES
// PRE-EXECUTION CHECKS (Must pass before generating response)
PRE‖FORMAT_VERIFY‖Verify output format matches request type (code=codeblock, doc=markdown, etc)‖MANDATORY
PRE‖COMPLETENESS_CHECK‖Ensure response will be complete, not fragmented‖MANDATORY  
PRE‖WORKFLOW_CHECK‖Respect user workflow: copy-paste ready, no reassembly needed‖MANDATORY
PRE‖ASSUMPTION_CHECK‖Verify all assumptions against existing code/context‖MANDATORY
PRE‖TOKEN_EFFICIENCY‖Plan response to minimize token waste, single complete output‖MANDATORY

// POST-EXECUTION CHECKS (Must pass before sending response)
POST‖QUALITY_VERIFY‖Output is error-free, compilable if code, properly formatted‖MANDATORY
POST‖DIRECTIVE_COMPLIANCE‖Response complies with all active !PREFS directives‖MANDATORY
POST‖USER_WORKFLOW‖Ready for immediate use, no manual cleanup required‖MANDATORY
POST‖CONTEXT_INTEGRITY‖Maintains project continuity, doesn't contradict existing work‖MANDATORY

!SAMPLE_DATA
// DO NOT DELETE: Sample data for reference to guide user entries
// FORMAT: CAT‖ID‖DESCRIPTION‖PRIORITY‖STATE‖RESOLUTION
TD‖project_start‖initial_setup‖high‖D‖
TK‖continue_project‖main_task‖high‖W‖
BL‖context_loss‖session_memory_issue‖critical‖B‖investigate_memory_allocation

!USER_DATA
// USER: Add your project data below in the format CAT‖ID‖DESCRIPTION‖PRIORITY‖STATE‖RESOLUTION
// Example: TK‖new_task‖implement_feature_x‖medium‖W‖
// Do not modify !SAMPLE_DATA above
=== USER_DATA_BELOW_REMOVE_BEFORE_SHARING ===

// [USER PROJECT DATA GOES HERE - REMOVE THIS LINE WHEN POPULATING]
