select front.PlaceID
from [Places] as front
where front.PlaceID <> 'W:out' and (not front.placeID like 'T%') and (not front.PlaceID like 'W:32:%') and
not exists
(select back.PlaceID 
 from [Places] as back
 where back.PlaceID = left(front.PlaceID,11)+'2')
