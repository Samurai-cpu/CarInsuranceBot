# Car Insurance Bot Documentation
## Setup instruction and dependencies
### Setup Instruction
1. Download source code
2. Do Database migration
3. Provide correct urls for: 
    - Mindee API
    - OpenAI API
    - Database
4. You can run bot
### Dependencies
- .NET 8 Runtime
- MSSQL server
- Access to OpenAI API
- Access to Mindee API
    - Custom Mindee API for extracting car title data with following fields
        - VechileIdentificationNumber (string)
        - YearModel (number)
        - Make (string)
        - BodyStyle (string)
        - TitleNumber (string)
        - PreviousTitleNumber (string)
        - TitleIssueDate (date with option extract only date chosen)
## Description of Bot's workflow
1. To strat interaction with bot user need send /start command
2. Bot requests user to upload passport image
3. User checks the passport data extracted from image and if he wants he can return to step 2
4. Bot requests user to upload car title image
5. User the title data extracted from image and if he wants he can return to step 4
6. If user confirms the data Bot will suggest to apply the car insurance which costs 100$
7. User can agree or disagree with this proposition, bot will inform user that the price is fixed at 100$
8. If user agrees he will get a dummy insurance data
9. User can finish interaction with bot anytime using /finish command (this command create for test purposes)
