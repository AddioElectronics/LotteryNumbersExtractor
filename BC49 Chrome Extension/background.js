
var autosave;

chrome.runtime.onInstalled.addListener(() => {
  console.log('BC49 Number Extractor Installed');
});

chrome.tabs.onUpdated.addListener( function (tabId, changeInfo, tab) {

  //Get autosave before content asks for it.
  chrome.storage.sync.get(['autosave'], function(result){
    if(result == null){
      chrome.storage.sync.set({'autosave' : false});
      autosave = false;
    }else{
      autosave = result.autosave;
    }
  });
});

chrome.runtime.onMessage.addListener(function(request, sender, sendResponse) {
  
  switch(request.method){
    case "setAutoSave" :
      chrome.storage.sync.set({'autosave' : request.value}, function(){});     
      sendResponse({});
      break;
    case "getAutoSave" :
      sendResponse({value: autosave});
      chrome.storage.sync.get(['autosave'], function(result){});       
      break;
    default:
      sendResponse({});
      break;
  }
  
});