Feature: Process Payment
  As a customer
  I want to pay for my order
  So that my order can be prepared

  Scenario: Payment approved with fake checkout
    Given an order exists with number "ORD-001" and amount 35.90
    And the customer name is "João PAGO"
    When I create a payment for the order
    Then the payment should be created successfully
    And the payment status should be "Approved"
    And a "PaymentApproved" event should be published

  Scenario: Payment rejected with fake checkout
    Given an order exists with number "ORD-002" and amount 50.00
    And the customer name is "Maria CANCELADO"
    When I create a payment for the order
    Then the payment should be created successfully
    And the payment status should be "Rejected"
    And a "PaymentRejected" event should be published

  Scenario: Payment pending with fake checkout
    Given an order exists with number "ORD-003" and amount 25.90
    And the customer name is "Carlos Silva"
    When I create a payment for the order
    Then the payment should be created successfully
    And the payment status should be "Pending"
    And a "PaymentPending" event should be published
