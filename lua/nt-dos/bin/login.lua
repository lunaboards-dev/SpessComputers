local auth = require("auth")

while true do
    print("Login")
    print("Press Control-Enter to show login prompt or swipe keycard to log in.")

    local ctx = auth.context()

    if not ctx:await() then
        
    end
end