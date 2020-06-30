$.fn.selectRange = function (start, end) {
    if (end === undefined) {
        end = start;
    }
    return this.each(function () {
        if ('selectionStart' in this) {
            this.selectionStart = start;
            this.selectionEnd = end;
        } else if (this.setSelectionRange) {
            this.setSelectionRange(start, end);
        } else if (this.createTextRange) {
            var range = this.createTextRange();
            range.collapse(true);
            range.moveEnd('character', end);
            range.moveStart('character', start);
            range.select();
        }
    });
};

class MFAValidator {
    constructor(ValId, type, payload, parentSelector, onError, onValid, onChange, valIdFieldname, secretFieldname, payloadFieldname) {
        this.valId = ValId;
        this.type = type;
        this.payload = payload;
        this.parentSelector = parentSelector;
        this.onError = onError;
        this.onValid = onValid;
        this.onChange = onChange;
        this.username = null;
        this.password = null;
        this.token = null;
        this.valid = false;
        this.valIdFieldname = valIdFieldname ?? "VerifyId";
        this.secretFieldname = secretFieldname ?? "ValidationCode";
        this.payloadFieldname = payloadFieldname; //if not provided, no payload field
        $(parentSelector).data("MFA", this); //assign to object for examination
        this.buildValidation(parentSelector);
    }
    validateCode() {
        var model = this;
        model.valid = false;
        var verificationCode = this.getVerificationCode(this.parentSelector + " .verificationContainer");
        if (!verificationCode) //empty cell
        {
            $(this.parentSelector + " .ValidationCode").val(""); //empty val
            return;
        }
        var ajaxData = {
            url: "/api/mfa/verify/" + this.valId,
            method: "post",
            data: {
                secret: verificationCode,
                payload: this.payload,
                type: this.type
            },
            success: function(data) {
                $(model.parentSelector + " .verificationContainer").addClass("valid").removeClass("invalid");
                $(model.parentSelector + " [data-valmsg-for=ValidationCode]").html("");
                var verString = "";
                $(model.parentSelector + " .verificationCharacter").each(function() {
                    verString += $(this).val();
                });
                $(model.parentSelector + " .ValidationCode").val(verString); //set hidden code input
                if (model.onValid)
                    model.onValid(data); //validate
                model.valid = true;
            },
            error: function(xhr, textStatus, errorThrown) {
                var error = xhr.responseText;
                $(model.parentSelector + " .verificationContainer").addClass("invalid").removeClass("valid");
                $(model.parentSelector + " [data-valmsg-for=ValidationCode]").html(error);
                $(model.parentSelector + " .ValidationCode").val(""); //empty val
                if (model.onError)
                    model.onError(xhr, textStatus, errorThrown);
            }
        };
        if (this.username != null) {
            ajaxData.data["username"] = this.username;
            ajaxData.data["password"] = this.password;
        }
        if (this.token != null) {
            ajaxData["beforeSend"] = (xhr) => {
                xhr.setRequestHeader("Authorization", "Bearer " + this.token);
            };
        }
        $.ajax(ajaxData);
    }

    buildValidation(parentSelector) {
        var model = this;
        var html = `<div class="verificationBody" style="display:flex; align-items: center; justify-content: space-evenly; border: 2px solid rgba(0, 0, 0, 0.20);  border-radius: 4px; margin-top: 10px; width: 100%;flex-wrap: wrap;padding: 10px;">\
            <div style="font-weight:bolder; font-size:125%;">\
                <span>Validation Code:</span>\
                <div class="text-muted" style="width: 140px;font-size: 56%;">The code will expire in 15 minutes.</div>\
            </div>\
            <input type="hidden" class="ValidationId" name="`+ this.valIdFieldname+`" value="`+ this.valId +`"/>\
            <div class="verificationContainer">\
                <input class="form-control verificationCharacter" type="text">\
                <span>-</span>\
                <input class="form-control verificationCharacter" type="text">\
                <span>-</span>\
                <input class="form-control verificationCharacter" type="text">\
                <span>-</span>\
                <input class="form-control verificationCharacter" type="text">\
                <span>-</span>\
                <input class="form-control verificationCharacter" type="text">\
            </div>\
            <input type="hidden" name="`+ this.secretFieldname + `" class="ValidationCode" />\
            `+ (this.payloadFieldname != null ? `<input type="hidden" name="` + this.payloadFieldname + `" value="` + this.payload + `" class="ValidationPayload" />` : "") +`\
            <div data-valmsg-for="` + this.secretFieldname + `" class="invalid-feedback" style="display:block;text-align:center"></div>\
        </div>`;
        $(parentSelector).html(html); //inject html
        $(document).on("input", parentSelector + " .verificationCharacter", function (e) {
            var element = $(e.currentTarget);
            element.val(element.val().toUpperCase());
            if (element.val().length >= 1) {
                element.val(element.val().slice(0, 1));
                var nextEl = element.next("span").next("input");
                nextEl.focus().selectRange(0);
                element.parent().removeClass("invalid valid");
            }
            else if (element.val().length == 0) {
                var lastEl = element.prev("span").prev("input");
                lastEl.focus().selectRange(1);
                element.parent().removeClass("invalid valid");
            }
            if (this.onChange)
                model.onChange();

            model.validateCode();
            //verify if all fields are full
        });
        $(document).on("keydown", this.parentSelector + " .verificationCharacter", function (e) {
            var element = $(e.currentTarget);
            if (e.which == 8 && element.val().length == 0) //backspace
            {
                element.parent().removeClass("invalid valid");
                var lastEl = element.prev("span").prev("input");
                lastEl.focus().selectRange(1);
            }
            this.valid = false;
        });
    }

    getVerificationCode() {
        var parent = this.parentSelector;
        var returnVar = "";
        var valid = true;
        $(parent).find(".verificationCharacter").each(function() {
            if ($(this).val() == "") //no empty blocks
                valid = false;
            returnVar += $(this).val().slice(0, 1);
        });
        if (valid)
            return returnVar;
        else
            return false;
    }
    setCredentials(username, password) {
        this.username = username;
        this.password = password;
    }
    setToken(token) {
        this.token = token;
    }
}
