# Testing Notes
# Testing Notes

1. the total tests are 16,  and 16 passed with xunit of 20

2. **Line coverage:** 90.5% 

3. **Where I used the libraries:**
   - I used Moq to replace the intefaces IInventoryService, IOrderRepository and
     INotificationService and also to check them with veeirfy .
   - I used AutoFixture to create random orders "but valid".
   - I used Bogus to create a mock 'realistic' name and email with fixed seed.

4. **Is it fair that 999.99 pays more than 1,000?**
   No it is not fair that 999.99 pays morre thatn 1000 becaue one buys for 1000
   pays only 900, so spending 0.0.1 more tamkes the orderrr 99.99 cheaper which
   is a dangerous flaw in discount rule

5. **Bug found:No there is no bug
