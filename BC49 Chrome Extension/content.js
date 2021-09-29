//If true, data will automatically save to a file when it is finished extracting.
//If false, a button will have to be pressed to initiate saving the file.
var autosave = false;

var data = [];
var rows = $('.win-number-table.row.no-brd-reduis').find('tr');
var combined = 'Data Numbers Bonus \n';
var files;
var filesProcessed = 0;
var totalFiles = -1;

$('body').append('<input id="autosave" class="extension-overlay" type="checkbox"><span class="extension-overlay">Auto-Export</span></input> ');

$('#autosave').on('change', function(){    
    var checked = $('#autosave').prop('checked');
    chrome.runtime.sendMessage({method: "setAutoSave", value:  checked}, function(response) {
    });
});

chrome.runtime.sendMessage({method: "getAutoSave"}, (response) => {
    var checked = response.value;
    $('#autosave').prop('checked', checked);
    autosave = checked;
});



$(document).ready(function(){
    if(autosave){
        Download(DataToCsv(data), window.location.href.replace('https://www.lotteryleaf.com/bc/bc-49/','') +'.csv', 'txt')
    }else{
        //Create button, after the data is extracted, which will be used to save data.
        $('body').append('<button id="export_data" type="button" class="extension-overlay">Export Numbers</button>');

        //Create event which will export data.
        $('button#export_data').click(function(){
            Download(DataToCsv(data), window.location.href.replace('https://www.lotteryleaf.com/bc/bc-49/','') +'.csv', 'txt')
        });
    }
});
  

//Extract data from website and put into "data"
for(var i = 1; i < rows.length; i++){

    var date = $($(rows[i]).find('td')[0]).text().replace(/\s/g, '');
    var numberlist = $(rows[i]).find('li');

    var numbers = '';
    for(var n = 0; n < numberlist.length - 1; n++){
        numbers += $(numberlist[n]).text().replace(/\s/g, '');
        if(n != numberlist.length - 2)
        numbers += ',';
    }

    var bonus = $(numberlist[numberlist.length - 1])
    .clone()    //clone the element
    .children() //select all the children
    .remove()   //remove all the children
    .end()  //again go back to selected element
    .text()
    .replace(/\s/g, '');


    
    data.push([date, numbers, bonus]);
}


//Create button, after the data is extracted, which will be used to save data.
 $('body').append('<button id="import_data" type="button" class="extension-overlay">Combine Numbers</button>');
 $('body').append('<input id="file_importer" type="file" multiple ></input>');


$('input#file_importer').on('change', function(e){

    //Get all filenames
    files = $('input#file_importer').get(0).files;
    totalFiles = files.length;

    //Start downloading the first file.
    //This function will recursively download the rest.
    UploadCombineDownload(files[0]);
});

//Create event which will import data and combine into one spreadsheet.
$('button#import_data').click(function(){
    $('input#file_importer').click();
});

// Function to download data to a file
function Download(data, filename, type) {
    var file = new Blob([data], {type: type});
    if (window.navigator.msSaveOrOpenBlob) // IE10+
        window.navigator.msSaveOrOpenBlob(file, filename);
    else { // Others
        var a = document.createElement("a"),
                url = URL.createObjectURL(file);
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        setTimeout(function() {
            document.body.removeChild(a);
            window.URL.revokeObjectURL(url);  
        }, 0); 
    }
}

//Convert data to csv string
function DataToCsv(data){

    var text = 'Data Numbers Bonus \n';

    data.forEach(function(value, index){
        text += value[0] + ' ' + value[1] + ' ' + value[2] + '\n'; 
    });

    return text;
}

//"Upload" a local numbers csv to the browser, to combine.
//When all files are combined, it will download.
function UploadCombineDownload(file){
    var fr = new FileReader();
    fr.readAsText(file);

     //Since readAsText is asynchronous we need to finish
    //when the file is done loading.
    fr.onload = function() {
        
        // break the textblock into an array of lines
        var lines = fr.result.split('\n');
        // remove one line, starting at the first position
        lines.splice(0,1);
        // join the array back into a single string
        combined += lines.join('\n');

        filesProcessed++;

        //Recursively upload the next file.
        //Or if all files have completed download the combined.
        if(filesProcessed != totalFiles)
            UploadCombineDownload(files[filesProcessed]);
        else
        Download(combined, 'allyears.csv', 'text')
    };        
}