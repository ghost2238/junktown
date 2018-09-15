// JSON proxy
<?php
function allowed($url) {
  foreach(['https://www.reddit.com'] as $domain)
    if(strpos($url, $domain) === 0)
      return true;
}
$url = $_GET['u'];
if(strpos($url, '/r/') === 0)
  $url = 'https://www.reddit.com' . $url;
if(!allowed($url))
  die('<!DOCTYPE html><head></head><body>Not allowed</body></html>');
header('content-type: application/json; charset=UTF-8');
echo file_get_contents($url);
?>