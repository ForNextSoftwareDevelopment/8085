; THE FOLLOWING PROGRAMS AND SUBROUTINES ARE DESCRIBED IN DETAIL
; IN INTEL CORPERATION/S APPLICATION NOTE AP-29, "USING THE 8085
; 5ERIAL I/O LINES". THE FIRST SECTION IS A GENERAL PURPOSE CRT
; INTERFACE WITH AUTOMATIC BAUD RATE IDENTIFICATION; THE SECOND 
; SECTION IS A MAGNETIC TAPE INTERFACE FOR STORING DATA ON CASSETTE
; TAPES. THE CODE PRESENTED HERE IS ORIGINED AT LOCATION 800H
; AND MIGHT BE PART OF AN EXPANSION PROM IN AN INTEL SDK-85
; SYSTEM DESIGN KIT.

BITTIME EQU 20C8H               ;ADDRESS OF STORAGE FOR COMPUTED BIT DELAY
HALFBIT EQU 20CAH               ;ADDRESS OF STORAGE FOR HALF BIT DELAY
BITSO   EQU 11                  ;DATA BITS PUT OUT (INCLUDING TWO STOP BITS)
BITSI   EQU 9                   ;DATA BITS TO BE RECEIVED (INCLUDING ONE STOP BIT)

ORG 0800H                       ;STARTING ADDRESS OF SDK-85 EXPANSION PROM

;CRTTST             CRT INTERFACE TEST. WHEN CALLED, AWAITS THE SPACE BAR BEING PRESSED ON
;                   THE SYSTEM CONSOLE, AND THEN RESPONDS WITH A DATA RATE VERIFICATION
;                   MESSAGE, THERE AFTER, CHARACTERS TYPED ON THE KEYBOARD ARE ECHOED
;                   ON THE DISPLAY TUBE. WHEN A BREAK KEY IS TYPED, THE ROUTINE IS
;                   RE-STARTED, ALLOWING A DIFFERENT BAUD RATE TO BE SELECTED ON THE CRT.
CRTTST:     LXI     SP,20C0H
CRT1:       MVI     A,0C0H      ;SOD MUST BE HIGH BETWEEN CHARACTERS
            SIM
            CALL    BRID        ;IDENTIFY DATA RATE USED BY TERMINAL
            CALL    SIGNON      ;OUTPUT SIGNON MESSAGE AT RATE DETECTED
ECHO:       CALL    CIN         ;READ NEXT KEYSTROKE INTO REGISTER C
            MOV     A,C
            ORA     A           ;CHECK IF CHARACTER WAS A <BREAK> (ASCII 00H)
            JZ      CRT1        ;IF SO, RE-IDENTIFY DATA RATE
                                ;THIS ALLOWS ANOTHER RATE TO BE SELECTED ON CRT
            CALL    COUT        ;OTHERWISE COPY REGISTER C TO THE SCREEN
            JMP     ECHO        ;CONTINUE INDEFINITELY (UNTIL BREAK)

;BRID               BAUD RATE IDENTIFICATION SUBROUTINE
;                   EXPECTS A <CR> (ASCII 20H) TO BE RECEIVED FROM THE CONSOLE
;                   THE LENGTH OF THE INITIAL ZERO LEVEL (SIX BITS WIDE) IS MEASURED
;                   IN ORDER TO DETERMINE THE DATA RATE FOR FUTURE COMMUNICATIONS.
BRID:       RIM                 ;VERIFY THAT THE "ONE" LEVEL HAS BEEN ESTABLISHED
            ORA     A           ;\ AS THE CRT IS POWERING UP
            JP      BRID
BRI1:       RIM                 ;MONITOR SID LINE STATUS
            ORA     A
            JM      BRI1        ;LOOP UNTIL START BIT IS RECEIVED
            LXI     H,-6        ;BIAS COUNTER USED IN DETERMINING ZERO DURATION
BRI3:       MVI     E,04H  
BRI4:       DCR     E           ;53 MACHINE CYCLE DELAY LOOP
            JNZ     BRI4
            INX     H           ;INCREMENT COUNTER EVERY 84 CYCLES WHILE SID IS LOW
            RIM 
            ORA     A
            JP      BRI3
                                ;<HL> NOW CORRESPONDS TO INCOMING DATA RATE
            PUSH    H           ;SAVE COUNT FOR HALFBIT TIME COMPUTATION
            INR     H           ;BITTIME IS DETERMINED BY INCREMENTING
            INR     L           ;\ H AND L INDIVIDUALLY
            SHLD    BITTIME
            POP     H           ;RESTORE COUNT FOR HALFBIT DETERMINATION
            ORA     A           ;CLEAR CARRY
            MOV     A,H         ;ROTATE RIGHT EXTENDED <HL>
            RAR                 ;\ TO DIVIDE COUNT BY 2
            MOV     H,A
            MOV     A,L
            RAR
            MOV     L,A
            INR     H           ;PUT H AND L IN PROPER FORMAT FOR DELAY
            INR     L           ;\ SEGMENTS (IMCREMENT EACH)
            SHLD    HALFBIT     ;SAVE AS HALF-BIT TIME DELAY PARAMETER
            RET

;SIGNON             WRITES A SIGN-ON MESSAGE TO THE CRT AT WHAT SHOULD BE THE CORRECT RATE.
;                   IF THE MESSAGE IS UNINTELLIGIBLE... WELL, S0 IT GOES.
SIGNON:     LXI     H, STRNG    ;LOAD START OF SIGN-ON MESSAGE
S1:         MOV     C,M         ;GET NEXT CHARACTER
            XRA     A           ;CLEAR ACCUMULATOR
            ORA     C           ;CHECK IF CHARACTER IS END OF STRING
            RZ                  ;RETURN IF SIGN-ON COMPLETE
            CALL    COUT        ;ELSE OUTPUT CHARACTER TO CRT
            INX     H           ;INDEX POINTER
            JMP     S1          ;ECHO NEXT CHARACTER

STRNG:      DB      0DH, 0AH    ;<CR><LF>
            DB      "BAUD RATE CHECK"
            DB      0DH, 0AH    ;<CR><LF>
            DB      00H         ;END-OF-STRING ESCAPE CODE

;COUT               CONSOLE OUTPUT SUBROUTINE
;                   WRITES THE CONTENTS OF THE C REGISTER TO THE CRT DISPLAY SCREEN
COUT:       DI
            PUSH    B
            PUSH    H
            MVI     B,BITSO     ;SET NUMBER OF BITS TO BE TRANSMITTED
            XRA     A           ;CLEAR CARRY
CO1:        MVI     A,80H       ;SET WHAT WILL BECOME SOD ENABLE BIT
            RAR                 ;MOVE CARRY INTO SOD DATA BIT OF ACC
            SIM                 ;OUTPUT DATA BIT TO SOD
            LHLD    BITTIME
CO2:        DCR     L           ;WAIT UNTIL APPROPRIATE TIME HAS PASSED
            JNZ     CO2
            DCR     H
            JNZ     CO2
            STC                 ;SET WHAT WILL EVENTUALLY BECOME A STOP BIT
            MOV     A,C         ;ROTATE CHARACTER RIGHT ONE BIT,
            RAR                 ;\ MOVING NEXT DATA BIT INTO CARRY
            MOV     C,A
            DCR     B           ;CHECK IF CHARACTER (AND STOP BIT(S)) DONE
            JNZ     CO1         ;IF NOT, OUTPUT CURRENT CARRY
            POP     H           ;RESTORE STATUS AND RETURN
            POP     B
            EI
            RET

;CIN                CONSOL INPUT SUBROUTINE WAITS FOR A KEYSTROKE AND
;                   RETURNS HITH 8 BITS IN REG C.
CIN:        DI
            PUSH    H
            MVI     B,BITSI     ;BATA BITS TO BE READ (LAST RETURNED IN CY)
CI1:        RIM                 ;WAIT FOR SYNC BIT TRANSITION
            ORA     A
            JM      CI1
            LHLD    HALFBIT
CI2:        DCR     L           ;WAIT UNTIL MIDDLE OF START BIT
            JNZ     CI2
            DCR     H
            JNZ     CI2
CI3:        LHLD    BITTIME     ;WAIT OUT BIT TIME
CI4:        DCR     L
            JNZ     CI4
            DCR     H
            JNZ     CI4
            RIM                 ;CHECK SID LINE LEVEL
            RAL                 ;DATA BIT IN CY
            DCR     B           ;DETERMINE IF THIS IS FIRST STOP BIT
            JZ      CI5         ;IF SO, JUMP OUT OF LOOP
            MOV     A,C         ;ELSE ROTATE INTO PARTIAL CHARACTER IN C
            RAR                 ;ACC HOLDS UPDATED CHARACTER
            MOV     C,A
            NOP                 ;EQUALIZES COUT AND CIN LOOP TIMES
            JMP     CI3
CI5:        POP     H
            EI
            RET                 ;CHARACTER COMPLETE

;*********************************************************************************
;           THE FOLLOWING CODE IS USED BY THE CASSETTE INTERFACE.
;           SUBROUTINES TAPEO AND TAPEIN ARE USED RESPECTIVELY
;           TO OUTPUT OR RECEIVE AN EIGHT BIT BYTE OF DATA. REGISTER C
;           HOLDS THE DATA IN EITHER CASE. REGISTERS A,B, &C ARE ALL DESTROYED.

CYCNO       EQU     16          ;TWICE THE NUMBER OF CYCLES PER TONE BURST
HALFCYC     EQU     24          ;DETERMINES TONE FREQUENCY
CKRATE      EQU     22          ;SETS SAMPLE RATE
LEADER      EQU     250         ;NUMBER OF SUCCESIVE TONE BURSTS COMPRISING LEADER
LDRCHK      EQU     250         ;USED IN PLAYBK TO VERIFY PRESENCE OF LEADER

;BLKRCD:            OUTPUTS A YERY LONG TONE BURST (<LEADER> TIMES
;                   THE NORMAL BURST BURATION) TO ALLOW RECORDER ELECTRONICS
;                   AND AGC TO STABILIZE, THEN OUTPUTS THE REMAINDER OF THE
;                   256 BYTE PAGE POINTED TO BY <H>, STARTING AT BYTE <L>.
BLKRCD:     MVI     C,LEADER    ;SET UP LEADER BURST LENGTH
            MVI     A,0C0H      ;SET ACCUMULATOR TO RESULT IN TONE BURST
BR1:        CALL    BURST       ;OUTPUT TONE
            DCR     C
            JNZ     BRI1        ;SUSTAIN LEADER TONE
            XRA     A           ;CLEAR ACCUMULATOR & OUTPUT SPACE, SO THAT
            CALL    BURST       ;\ START OF FIRST DATA BYTE CAN BE DETECTED
BR2:        MOV     C,M         ;GET DATA BYTE TO BE RECORDED
            CALL    TAPEO       ;OUTPUT REGISTER C TO RECORDER
            INR     L           ;POINT TO NEXT BYTE 
            JNZ     BR2
            RET                 ;AFTER BLOCK IS COMPLETE

;TAPEO              OUTPUTS THE BYTE IN REGISTER C TO THE RECORDER.
;                   REGISTERS A,B,C,D, &E ARE ALL USED
TAPEO:      DI
            PUSH    D           ;D&E USED AS COUNTERS BY SUBROUTINE BURST
            MVI     B,9         ;WILL RESULT IN 8 DATA BITS AND ONE STOP BIT
TO1:        XRA     A           ;CLEAR ACCUMULATOR
            MVI     A, 0C0H     ;SET ACCUMULATOR TO CAUSE A TONE BURST
            CALL    BURST
            MOV     A,C         ;MOVE NEXT DATA BIT INTO THE CHRRY
            RAR
            MOV     C,A         ;CARRY WILL BECOME SOD ENABLE IN BURST ROUTINE
            MVI     A,01H       ;SET BIT TO BE REPEATEDLY COMPLEMENTED IN BURST
            RAR
            RAR
            CALL    BURST       ;OUTPUT EITHER A TONE OR A PAUSE
            XRA     A           ;CLEAR ACCUMULATOR
            CALL    BURST       ;OUTPUT PAUSE
            DCR     B
            JNZ     TO1         ;REPEAT UNTIL BYTE FINISHED 
            POP     D           ;RESTORE STATUS AND RETURN
            EI
            RET

BURST:      MVI     D,CYCNO     ;SET NUMBER OF CYCLES
BU1:        SIM                 ;COMPLEMENT SOD LINE IF SOD ENABLE BIT SET
            MVI     E,HALFCYC
BU2:        DCR     E           ;REGULATE TONE FREQUENCY
            JNZ     BU2
            XRI     80H         ;COMPLEMENT SOD DATA BIT IN ACCUMULATOR
            DCR     D
            JNZ     BU1         ;CONTINUE UNTIL BURST (OR EQUIVILENT PAUSE) FINISHED
            RET

;PLAYBK             WAITS FOR THE LONG LEADER BURST TO ARRIVE, THEN CONTINUES
;                   READING BYTES FROM THE RECORDER AND STORING THEM
;                   IN MEMOPY STARTING AT LOCATION <HL>.
;                   CONTINUES UNTIL THE END OF THE CURRENT PAGE (<L>=0FFH) IS REACHED.
PLAYBK:     MVI     C,LDRCHK    ;<LDRCHK> SUCCESSIVE HIGHS MUST BE READ
PB1:        CALL    BITIN       ;\ TO YERIFY THAT THE LEADER IS PRESENT
            JNC     PLAYBK      ;\ AND ELECTRONICS HAS STABILIZED
            DCR     C 
            JNZ     PB1
PB2:        CALL    TAPEIN      ;GET DATA BYTE FROM RECORDER 
            MOV     M,C         ;STORE IN MEMORY
            INR     L           ;INCREMENT POINTER
            JNZ     PB2         ;REPEAT FOR REST OF CURRENT PAGE
            RET 

;TAPEIN             CASSETTE TAPE INPUT SUBROUTINE. READS ONE BYTE OF DATA
;                   FROM THE RECORDER INTERFACE AND RETURNS WITH THE BYTE IN REGISTER C.
TAPEIN:     MVI     B,9         ;READ EIGHT DATA BITS
TI1:        MVI     D,00H       ;CLEAR UP/DOWN COUNTER
TI2:        DCR     D           ;DECREMENT COUNTER EACH TIME ONE LEVEL IS READ
            CALL    BITIN
            JC      TI2         ;REPEAT IF STILL AT ONE LEVEL
            CALL    BITIN 
            JC      TI2
TI3:        INR     D           ;INCREMENT COUNTER EACH TIME ZERO IS READ
            CALL    BITIN
            JNC     TI3         ;REPEAT EACH TIME ZERO IS READ
            CALL    BITIN
            JNC     TI3
            MOV     A,D
            RAL                 ;MOVE COUNTER MOST SIGNIFICANT BIT INTO CARRY
            MOV     A,C
            RAR                 ;MOVE DATA BIT RECEIVED (CY) INTO BYTE REGISTER
            MOV     C,A
            DCR     B
            JNZ     TI1         ;REPEAT UNTIL FULL BYTE ASSEMBLED
            RET

BITIN:      MVI     E,CKRATE
BI1:        DCR     E
            JNZ     BI1         ;LIMIT INPUT SAMPLING RATE
            RIM                 ;SAMPLE SID LINE
            RAL                 ;MOVE DATA INTO CY BIT
            RET
            END


