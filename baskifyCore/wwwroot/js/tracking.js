class Tracker{
    constructor(basketId, token, parentSelector){
        this.basketId = basketId;
        this.token = token;
        this.stageNum = 100;
        this.data = null;
        this.parentSelector = parentSelector;
    }
    
    submitTracking(e, model){
        e.preventDefault();
        var formdata = new FormData(e.target);
        var ajaxInput ={
            url: "https://localhost:44331/api/baskets/track/"+this.basketId,
            method: "post",
            data: formdata,
            contentType: false,
            processData: false,
            error: function(xhr, textStatus, errorThrown) {
                $(model.parentSelector + " .trackingEditor .errorMessage").html(xhr.responseText);
                $(model.parentSelector + " .trackingEditor .alert").css("display", "flex");
            },
            success: function(data){
                model.buildTracker();
            }
        }
        
        if (this.token != null) {
            ajaxInput["beforeSend"] = (xhr) => {
                xhr.setRequestHeader("Authorization", "Bearer " + this.token);
            };
        }
        
        $.ajax(ajaxInput);
    }
    
    onMouseEnter(e, model){
        var id = $(e.currentTarget).data("id");
        if(id == null){
            id = $(e.currentTarget).closest(".stage").data("id");
        }
        var event = model.getEvent(id);
        var html = `<div class="infoItem"><span>Time: </span><span>${moment(event.time).format("MMMM Do YYYY, h:mm:ss a")}</span></div>\
                    <div class="infoItem"><span>Location: </span><span>${event.location}</span></div>\
                    <div class="infoItem"><span>Status: </span><span>${event.message}</span></div>`;
        $(this.parentSelector+" .stageInfo").html(html).addClass("Visible");
        $(this.parentSelector+" .infoData").css("display", "none");
    }
    
    getEvent(id){
        for(var update of this.data.updates){
            if(update.id == id)
                return update;
        }
    }
    
    createStage(event){
        return(`<div class="stage" data-id="${event.id}" style="z-index: ${event.id}" >\
                    <svg class="arrow" width="15" height="40" version="1.1" xmlns="http://www.w3.org/2000/svg">\
                      <polygon points="0 0 0 40 15 0"\
                          fill="green"/>\
                    </svg>\
                </div>`);
    }
    
    getIconLogo(type){
        switch(type){
            case 0: //ups
                return "/Content/upsLogo.png";
            case 1: //usps
                return "/Content/uspsLogo.png";
            case 2: //fedEx
                return "/Content/fedExLogo.png";
        }
    }
    
    buildEditor(trackingNumber, carrier){
        var model=this;
        var html = `<div class="trackingEditor" style="justify-content: center;">\
                    ${trackingNumber != null? `<div class="exitButton">&#10006;</div>`: ""}\
                    <div class="alert alert-dismissible alert-danger" style="display:none;width: calc(70% + 10px); margin-top:20px">\
                      <span class="errorMessage"></span>\
                    </div>\
                    <form style="width: 100%" class="editTrackingForm">\
                        <div class="form-group" style="display:flex; width: 100%; justify-content: center">\
                            <select name="Carrier" class="custom-select" style="width: 30%; margin-right: 10px">\
                                <option>Select a Carrier</option>\
                                <option value="0" ${carrier == 0? `selected`: ''}>UPS</option>\
                                <option value="1" ${carrier == 1? `selected`: ''}>USPS</option>\
                                <option value="2" ${carrier == 2? `selected`: ''}>FedEx</option>\
                            </select>\
                            <input class="form-control" name="TrackingNumber" style="width: 40%" placeholder="Tracking Number" value=${trackingNumber?? ""}>\
                        </div>\
                        <div class="form-group" style="display:flex; width: 100%; justify-content: center">\
                            <button type="submit" class="btn btn-success" style="width: calc(40% + 10px)">Update Tracking</button>\
                            <button type="button" class="btn btn-info markDelivered" style="width: 30%">Mark as Delivered</button>\
                        </div>\
                    </form>\
                </div>`;
        
        $(model.parentSelector).html(html).promise().done(function(){
            var youSure = false;
            var timeout = null;

            $(model.parentSelector + " .markDelivered").on("click", function(){
                if(!youSure){
                    $(this).html("You Sure?");
                    youSure = true;
                    timeout = setTimeout(() => {$(this).html("Mark as Delivered"); youSure = false}, 4000);
                    return;
                }
                clearTimeout(timeout);

                var ajaxInput ={
                    url: "https://localhost:44331/api/baskets/delivered/"+model.basketId,
                    method: "post",
                    error: function(xhr, textStatus, errorThrown) {
                        $(model.parentSelector + " .trackingEditor .errorMessage").html(xhr.responseText);
                        $(model.parentSelector + " .trackingEditor .alert").css("display", "flex");
                    },
                    success: function(data){
                        model.buildTracker();
                    }
                }

                if (model.token != null) {
                    ajaxInput["beforeSend"] = (xhr) => {
                        xhr.setRequestHeader("Authorization", "Bearer " + model.token);
                    };
                }

                $.ajax(ajaxInput);
            });
            
            $(model.parentSelector + " .trackingEditor").on("submit", ".editTrackingForm", (e) => model.submitTracking(e, model)); //add hover listener
            $(model.parentSelector + " .trackingEditor").on("click", ".exitButton", (e) => {model.buildTracker()});
        }); //mark delivered
    }
    
    buildDisputer(){
        var model=this;
        var html = `<div class="disputeEditor" style="justify-content: center;">\
                    <div class="exitButton">&#10006;</div>\
                    <div class="alert alert-dismissible alert-danger" style="display:none;width: 80%; margin: unset">\
                      <span class="errorMessage"></span>\
                    </div>\
                    <textarea rows="5" class="form-control" name="message" style="width: 80%; height: 80%" placeholder="Explain what's wrong with the shipment"/>\
                    <button type="button" class="btn btn-info disputeButton" style="width: 80%">Submit Dispute</button>\
                </div>`;
        
        $(model.parentSelector).html(html).promise().done(function(){
            var youSure = false;
            var timeout = null;

            $(model.parentSelector + " .disputeButton").on("click", function(){
                if(!youSure){
                    $(this).html("You Sure?");
                    youSure = true;
                    timeout = setTimeout(() => {$(this).html("Submit Dispute"); youSure = false}, 4000);
                    return;
                }
                clearTimeout(timeout);

                var ajaxInput ={
                    url: "https://localhost:44331/api/baskets/dispute/"+model.basketId,
                    method: "post",
                    data: {message: $(model.parentSelector + " .disputeEditor [name=message]").val()},
                    error: function(xhr, textStatus, errorThrown) {
                        $(model.parentSelector + " .disputeEditor .errorMessage").html(xhr.responseText);
                        $(model.parentSelector + " .disputeEditor .alert").css("display", "flex");
                    },
                    success: function(data){
                        model.buildTracker();
                    }
                }

                if (model.token != null) {
                    ajaxInput["beforeSend"] = (xhr) => {
                        xhr.setRequestHeader("Authorization", "Bearer " + model.token);
                    };
                }

                $.ajax(ajaxInput);
            });
            $(model.parentSelector + " .disputeEditor").on("click", ".exitButton", (e) => {model.buildTracker()});
        }); //mark delivered
    }
    
    buildTracker(){
        var model = this;
        var ajaxInput = {
            url: "https://localhost:44331/api/baskets/track/"+this.basketId,
            method: "get",
            success: function(data) {
                model.data = data;
                
                var stages = "";
                for(var update of data.updates){
                    stages = model.createStage(update) + stages;
                }
                
                var html = `<div class="TrackingContainer">\
                                 ${data.editable?`<div class="exitButton"><img src="/Content/edit.svg" style="height:20px; margin-right:5px; margin-top:5px"></div>`: ""}\
                                ${!data.editable && data.disputeable?`<div class="exitButton" data-toggle="tooltipTracking" data-selector=".TrackingContainer" data-placement="top" title="Dispute Shipment"><img src="/Content/error.svg" style="height:20px; margin-right:5px; margin-top:5px"></div>`: ""}\
                                <div class="infoContainer">\
                                    <img class="brandLogo" src="${model.getIconLogo(data.carrier)}">\
                                    <div class="infoData">\
                                        <div class="trackingNumber infoItem"><span>Tracking Number: </span><span>${data.trackingNumber}</span></div>\
                                        <div class="shipmentDate infoItem"><span>Shipment Date: </span><span>${moment(data.updates[data.updates.length-1].time).format("MMMM Do YYYY, h:mm:ss a")}</span></div>\
                                        <div class="origin infoItem"><span>Origin: </span><span>${data.origin}</span></div>\
                                        <div class="status infoItem"><span>Status: </span><span>${data.disputed? `<span style="color:red; display:inline-flex; align-items:center">Disputed (${data.disputeText}) ${data.canCancelDispute? `<img src="/Content/close.svg" class="cancelDispute" style="height:.8rem; padding-left:5px; cursor:pointer" data-toggle="tooltipTracking" data-selector=".TrackingContainer" data-placement="top" title="Cancel Dispute">`: ``}</span>` : (data.updates[0].message + ` (${moment(data.updates[0].time).fromNow()})`)}</span></div>\
                                    </div>\
                                    <div class="stageInfo"></div>
                                </div>\
                                <div class="StatusBar ${data.delivered? "Delivered" : ""}">\
                                    ${stages}
                                </div>`;
                
                if(data.trackingNumber == null){
                    html = `<div class="TrackingContainer">\
                                ${!data.editable && data.disputeable?`<div class="exitButton" data-toggle="tooltipTracking" data-selector=".TrackingContainer" data-placement="top" title="Dispute Shipment"><img src="/Content/error.svg" style="height:20px; margin-right:5px; margin-top:5px"></div>`: ""}\
                                <div class="infoContainer">\
                                    <div class="infoData">\
                                        <div class="status infoItem"><span>Status: </span><span>${data.disputed? `<span style="color:red; display:inline-flex; align-items:center">Disputed (${data.disputeText}) ${data.canCancelDispute? `<img src="/Content/close.svg" class="cancelDispute" style="height:.8rem; padding-left:5px; cursor:pointer" data-toggle="tooltipTracking" data-selector=".TrackingContainer" data-placement="top" title="Cancel Dispute">`: ``}</span>` : (data.updates[0].message + ` (${moment(data.updates[0].time).fromNow()})`)}</span></div>\
                                    </div>\
                                    <div class="stageInfo"></div>
                                </div>\
                            </div>`;
                }
                
                $(model.parentSelector).html(html).promise().done(function(){
                    $(model.parentSelector + " .TrackingContainer .exitButton").on("click", (e) => {if(data.editable) model.buildEditor(data.trackingNumber, data.carrier); else if (data.disputeable) model.buildDisputer()});
                    $(model.parentSelector + " .TrackingContainer").on("mouseover", ".stage", (e) => model.onMouseEnter(e, model)); //add hover listener
                    $(model.parentSelector + " .TrackingContainer").on("mouseover", ".arrow", (e) => model.onMouseEnter(e, model)); //add hover listener
                    $(model.parentSelector + " .TrackingContainer").on("mouseout", ".StatusBar", (e) => {if($(e.target).parent(".stage").length != 0) return; $(".stageInfo").removeClass("Visible"); $(".infoData").removeAttr("style");});
                    $(model.parentSelector + " .TrackingContainer .cancelDispute").on("click", (e) => {
                        var ajaxInput ={
                            url: "https://localhost:44331/api/baskets/dispute/"+model.basketId,
                            method: "delete",
                            error: function(xhr, textStatus, errorThrown) {
                                model.buildTracker();
                            },
                            success: function(data){
                                model.buildTracker();
                            }
                        }

                        if (model.token != null) {
                            ajaxInput["beforeSend"] = (xhr) => {
                                xhr.setRequestHeader("Authorization", "Bearer " + model.token);
                            };
                        }

                        $.ajax(ajaxInput);
                    }); //add cancel dispute listener
                    $(function () {
                      $('[data-toggle="tooltipTracking"]').tooltip({trigger : 'hover'})
                    });
                });
                
            },
            error: function(xhr, textStatus, errorThrown) {
                if((xhr.status == 404 || xhr.status == 426) && JSON.parse(xhr.responseText).editable) //no tracking number, load editor OR critical error, load editor
                    model.buildEditor(null, null);
                else{
                    var html = `<div class="TrackingContainer">\
                                    <div>${xhr.status != 404? "Error retrieving tracking information, sorry!" : "Basket not yet shipped/delivered!"}</div>\
                                </div>`;
                    $(model.parentSelector).html(html);
                }
                    
            }
        }
        
        if (this.token != null) {
            ajaxInput["beforeSend"] = (xhr) => {
                xhr.setRequestHeader("Authorization", "Bearer " + this.token);
            };
        }
        
        $.ajax(ajaxInput);
    }
}