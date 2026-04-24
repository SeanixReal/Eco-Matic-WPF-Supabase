-- Supabase/Postgres seed for the shared Eco-Matic item catalog.
-- This file is safe to run multiple times: items are inserted only when their name is missing.
-- It seeds the global catalog only. Machine slot assignments are handled separately in the app.

with seed(name, type, price, calories, image_path, dispense_message, examine_message) as (
    values
        ('Mr Chips', 'Snack', 30.50, 160, 'Assets/Images/MrChips.png', 'Crunch away! Enjoy your Mr Chips!', 'A classic corn chip snack packed with cheesy, savory goodness.'),
        ('Nova', 'Snack', 40.00, 180, 'Assets/Images/Nova.png', 'Grab a multigrain bite!', 'A healthy, multigrain snack with a distinctive wave shape.'),
        ('Piattos', 'Snack', 35.00, 150, 'Assets/Images/Piattos.png', 'Hexagonal crunch time! Enjoy your Piattos!', 'Savory hexagon-shaped potato crisps coated in delicious seasoning.'),
        ('Chippy', 'Snack', 32.00, 170, 'Assets/Images/Chippy.png', 'Time for a barbecue blast! Enjoy your Chippy!', 'Iconic barbecue-flavored corn chips with a hearty crunch.'),
        ('Roller Coaster', 'Snack', 28.50, 140, 'Assets/Images/RollerCoaster.png', 'Have a fun ride with Roller Coaster rings!', 'Fun, cheese-flavored potato rings that loop around your fingers.'),
        ('Cheese Ring', 'Snack', 30.00, 160, 'Assets/Images/CheeseRing.png', 'Cheesy goodness coming right up!', 'Light and airy cheese-flavored puffed corn rings.'),
        ('Coca Cola', 'Drink', 30.50, 0, 'Assets/Images/CocaCola.png', 'Enjoy your ice-cold Coke! Stay refreshed!', 'The classic, iconic fizzy cola drink known worldwide.'),
        ('Pepsi', 'Drink', 30.00, 0, 'Assets/Images/Pepsi.png', 'Pop it open and enjoy the bold taste of Pepsi!', 'A sweet, slightly citrusy classic cola beverage.'),
        ('RC Cola', 'Drink', 25.00, 0, 'Assets/Images/RCCola.png', 'Refresh yourself with an RC Cola!', 'A crisp, refreshing cola with a smooth finish.'),
        ('Sting', 'Drink', 27.50, 0, 'Assets/Images/Sting.png', 'Power up! Here is your Sting energy!', 'A bright red, strawberry-flavored energy drink to keep you energized.'),
        ('Bandaid Box', 'Misc', 20.00, 0, 'Assets/Images/BandaidBox.png', 'Ouch! Hope it heals quickly!', 'A small box of sterile adhesive bandages for minor cuts.'),
        ('Eco Bag', 'Misc', 30.75, 0, 'Assets/Images/EcoBag.png', 'Thank you for loving the Earth! Happy carrying!', 'A reusable, eco-friendly tote bag designed to reduce plastic waste.')
)
insert into public.items (name, type, price, calories, image_path, dispense_message, examine_message)
select
    seed.name,
    seed.type,
    seed.price,
    seed.calories,
    seed.image_path,
    seed.dispense_message,
    seed.examine_message
from seed
where not exists (
    select 1
    from public.items existing
    where existing.name = seed.name
);
