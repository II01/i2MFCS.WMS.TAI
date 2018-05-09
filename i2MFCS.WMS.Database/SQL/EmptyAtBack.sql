select front.PlaceID
from [Places] as front
where front.PlaceID <> 'W:out' and 
not exists
(select back.PlaceID 
 from [Places] as back
 where back.PlaceID = left(front.PlaceID,11)+'2')
